/* =====================================================================
   Лекция 5. Хранимые процедуры. Создание процедур.
   База: Archive_Muzi (учёт аренды музыкального оборудования)

   Процедуры:
     1) usp_ShowAvailableEquipment  — без параметров
     2) usp_ShowRentalsByClient     — с одним параметром
     3) usp_AddClient               — проверка дубля телефона,
                                      OUTPUT-параметр, TRY...CATCH
     4) usp_AddPayment              — проверка аренды и суммы,
                                      транзакция + TRY...CATCH
     5) usp_CloseRental             — изменяет две таблицы,
                                      транзакция + TRY...CATCH
   ===================================================================== */

USE Archive_Muzi;
GO

/* =====================================================================
   1. ПРОЦЕДУРА БЕЗ ПАРАМЕТРОВ
   Каждое утро оператор смотрит, какое оборудование свободно.
   ===================================================================== */
CREATE OR ALTER PROCEDURE dbo.usp_ShowAvailableEquipment
AS
BEGIN
    SET NOCOUNT ON;

    SELECT  e.equipment_id,
            e.name,
            e.brand,
            e.model,
            e.rental_price,
            c.name AS category_name
    FROM    dbo.Equipment e
    JOIN    dbo.EquipmentCategories c ON c.category_id = e.category_id
    WHERE   e.status = N'Доступно'
    ORDER BY c.name, e.name;
END;
GO


/* =====================================================================
   2. ПРОЦЕДУРА С ОДНИМ ПАРАМЕТРОМ
   Оператору нужны аренды не всех клиентов, а одного выбранного.
   ===================================================================== */
CREATE OR ALTER PROCEDURE dbo.usp_ShowRentalsByClient
    @client_id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT  r.rental_id,
            cl.full_name AS client_name,
            r.rental_date,
            r.start_date,
            r.planned_end_date,
            r.actual_end_date,
            r.status
    FROM    dbo.Rentals r
    JOIN    dbo.Clients cl ON cl.client_id = r.client_id
    WHERE   r.client_id = @client_id
    ORDER BY r.start_date DESC;
END;
GO


/* =====================================================================
   3. ПРОЦЕДУРА ДОБАВЛЕНИЯ КЛИЕНТА
      - проверка дублирующего телефона (пункт 3 задания)
      - OUTPUT-параметр с ID созданного клиента (пункт 4 задания)
      - TRY...CATCH (пункт 6 задания)
   ===================================================================== */
CREATE OR ALTER PROCEDURE dbo.usp_AddClient
    @full_name     NVARCHAR(150),
    @phone         NVARCHAR(20),
    @email         NVARCHAR(100) = NULL,
    @address       NVARCHAR(255) = NULL,
    @new_client_id INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    /* пока клиент не создан, OUTPUT-параметр пустой */
    SET @new_client_id = NULL;

    BEGIN TRY
        /* ФИО обязательно */
        IF @full_name IS NULL OR LTRIM(RTRIM(@full_name)) = N''
            RAISERROR(N'ФИО клиента не может быть пустым.', 16, 1);

        /* телефон обязателен */
        IF @phone IS NULL OR LTRIM(RTRIM(@phone)) = N''
            RAISERROR(N'Телефон клиента не может быть пустым.', 16, 1);

        /* проверка дублирующего телефона: не ошибка, а сообщение */
        IF EXISTS (SELECT 1 FROM dbo.Clients WHERE phone = @phone)
        BEGIN
            PRINT N'Клиент с таким телефоном уже существует. Новая запись не создана.';
            RETURN;
        END

        INSERT INTO dbo.Clients
            (full_name, phone, email, address, registration_date, status)
        VALUES
            (@full_name, @phone, @email, @address, CAST(GETDATE() AS DATE), N'Активен');

        /* ID только что созданной строки возвращаем через OUTPUT */
        SET @new_client_id = SCOPE_IDENTITY();

        PRINT N'Клиент добавлен.';
    END TRY
    BEGIN CATCH
        SELECT  ERROR_NUMBER()  AS ErrorNumber,
                ERROR_MESSAGE() AS ErrorMessage,
                ERROR_LINE()    AS ErrorLine;
    END CATCH
END;
GO


/* =====================================================================
   4. ПРОЦЕДУРА ДОБАВЛЕНИЯ ПЛАТЕЖА (пункт 5 задания)
      - аренда должна существовать
      - сумма должна быть больше нуля
      - транзакция + TRY...CATCH
   ===================================================================== */
CREATE OR ALTER PROCEDURE dbo.usp_AddPayment
    @rental_id      INT,
    @amount         DECIMAL(18,2),
    @payment_type   NVARCHAR(30) = N'Оплата аренды',
    @payment_method NVARCHAR(30) = N'Карта',
    @new_payment_id INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SET @new_payment_id = NULL;

    BEGIN TRY
        /* проверка 1: аренда должна существовать */
        IF NOT EXISTS (SELECT 1 FROM dbo.Rentals WHERE rental_id = @rental_id)
            RAISERROR(N'Аренда с номером %d не найдена.', 16, 1, @rental_id);

        /* проверка 2: сумма должна быть больше нуля */
        IF @amount IS NULL OR @amount <= 0
            RAISERROR(N'Сумма платежа должна быть больше нуля.', 16, 1);

        BEGIN TRANSACTION;

            INSERT INTO dbo.Payments
                (rental_id, payment_date, amount, payment_type, payment_method, status)
            VALUES
                (@rental_id, CAST(GETDATE() AS DATE), @amount,
                 @payment_type, @payment_method, N'Оплачен');

            SET @new_payment_id = SCOPE_IDENTITY();

        COMMIT TRANSACTION;

        PRINT N'Платёж добавлен.';
    END TRY
    BEGIN CATCH
        /* если транзакция успела открыться — откатываем её */
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        SELECT  ERROR_NUMBER()  AS ErrorNumber,
                ERROR_MESSAGE() AS ErrorMessage,
                ERROR_LINE()    AS ErrorLine;
    END CATCH
END;
GO


/* =====================================================================
   5. ПРОЦЕДУРА ЗАВЕРШЕНИЯ АРЕНДЫ
      Меняет сразу две таблицы: закрывает аренду и возвращает
      оборудование в статус «Доступно». Обе операции должны выполниться
      вместе, поэтому они внутри одной транзакции.
   ===================================================================== */
CREATE OR ALTER PROCEDURE dbo.usp_CloseRental
    @rental_id INT,
    @end_date  DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SET @end_date = ISNULL(@end_date, CAST(GETDATE() AS DATE));

    BEGIN TRY
        IF NOT EXISTS (SELECT 1 FROM dbo.Rentals WHERE rental_id = @rental_id)
            RAISERROR(N'Аренда с номером %d не найдена.', 16, 1, @rental_id);

        IF EXISTS (SELECT 1 FROM dbo.Rentals
                   WHERE rental_id = @rental_id AND status = N'Завершена')
            RAISERROR(N'Аренда с номером %d уже завершена.', 16, 1, @rental_id);

        BEGIN TRANSACTION;

            UPDATE dbo.Rentals
               SET actual_end_date = @end_date,
                   status          = N'Завершена'
             WHERE rental_id = @rental_id;

            UPDATE e
               SET e.status = N'Доступно'
              FROM dbo.Equipment e
              JOIN dbo.RentalItems ri ON ri.equipment_id = e.equipment_id
             WHERE ri.rental_id = @rental_id;

        COMMIT TRANSACTION;

        PRINT N'Аренда закрыта, оборудование снова доступно.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        SELECT  ERROR_NUMBER()  AS ErrorNumber,
                ERROR_MESSAGE() AS ErrorMessage,
                ERROR_LINE()    AS ErrorLine;
    END CATCH
END;
GO


/* =====================================================================
   Проверка: какие процедуры созданы
   ===================================================================== */
SELECT  name AS ProcedureName,
        create_date,
        modify_date
FROM    sys.procedures
WHERE   name LIKE 'usp_%'
ORDER BY name;
GO

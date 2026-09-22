/* =====================================================================
   Лекция 5. Хранимые процедуры. Задания 4–6 базы Archive_Muzi.
   Пункт 7 (корректный и ошибочный запуск) идёт сразу после каждой
   процедуры — блоки «ПРОВЕРКА».

   В SSMS выполнять блоки ПО ОЧЕРЕДИ (выделить блок -> F5):
   ошибочные запуски специально прерывают пакет.

   Задание 4 опирается на usp_AddClient из задания 3, задание 6 —
   на usp_AddPayment из задания 5, поэтому они идут в этом порядке.
   ===================================================================== */

USE Archive_Muzi;
GO


/* #####################################################################
   ЗАДАНИЕ 4. OUTPUT-ПАРАМЕТР С ID СОЗДАННОГО КЛИЕНТА
   Пересоздаём ту же процедуру в дополненном виде: после INSERT
   приложению нужен номер новой записи — возвращаем его через OUTPUT.
   Заодно добавляем TRY...CATCH (задание 6) и проверку пустого ФИО.
   ##################################################################### */

IF OBJECT_ID('dbo.usp_AddClient', 'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_AddClient;
GO

CREATE PROCEDURE dbo.usp_AddClient
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
        IF @full_name IS NULL OR LTRIM(RTRIM(@full_name)) = N''
            RAISERROR(N'ФИО клиента не может быть пустым.', 16, 1);

        IF @phone IS NULL OR LTRIM(RTRIM(@phone)) = N''
            RAISERROR(N'Телефон клиента не может быть пустым.', 16, 1);

        IF EXISTS (SELECT 1 FROM dbo.Clients WHERE phone = @phone)
        BEGIN
            PRINT N'Клиент с таким телефоном уже существует. Новая запись не создана.';
            RETURN;
        END

        INSERT INTO dbo.Clients
            (full_name, phone, email, address, registration_date, status)
        VALUES
            (@full_name, @phone, @email, @address, CAST(GETDATE() AS DATE), N'Активен');

        /* ID только что созданной строки */
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

/* --- ПРОВЕРКА 4.1. Корректный запуск: возвращается ID ---------------- */
DECLARE @id INT;

EXEC dbo.usp_AddClient
     @full_name     = N'Орлов Илья Викторович',
     @phone         = N'+79995556677',
     @email         = N'ilya.orlov@example.com',
     @new_client_id = @id OUTPUT;

SELECT @id AS CreatedClientID;

SELECT client_id, full_name, phone, registration_date, status
FROM   dbo.Clients
WHERE  client_id = @id;
GO

/* --- ПРОВЕРКА 4.2. Ошибочный запуск: телефон уже занят --------------- */
/* Ожидается: сообщение о дубликате, OUTPUT-параметр остаётся NULL. */
DECLARE @id2 INT;

EXEC dbo.usp_AddClient
     @full_name     = N'Дубликат Телефона Иванович',
     @phone         = N'+79995556677',
     @new_client_id = @id2 OUTPUT;

SELECT @id2 AS CreatedClientID;      -- NULL: клиент не создан
GO

/* --- ПРОВЕРКА 4.3. Ошибочный запуск: пустое ФИО ---------------------- */
/* RAISERROR внутри TRY сразу передаёт управление в CATCH, поэтому
   вместо «красной» ошибки процедура возвращает таблицу с её описанием. */
DECLARE @id3 INT;

EXEC dbo.usp_AddClient
     @full_name     = N'   ',
     @phone         = N'+79990009999',
     @new_client_id = @id3 OUTPUT;

SELECT @id3 AS CreatedClientID;      -- NULL: клиент не создан
GO


/* #####################################################################
   ЗАДАНИЕ 5. ПРОВЕРКИ: АРЕНДА ДОЛЖНА СУЩЕСТВОВАТЬ, СУММА > 0
   Первая версия процедуры добавления платежа — только проверки,
   без TRY...CATCH (его добавим в задании 6).
   ##################################################################### */

IF OBJECT_ID('dbo.usp_AddPayment', 'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_AddPayment;
GO

CREATE PROCEDURE dbo.usp_AddPayment
    @rental_id      INT,
    @amount         DECIMAL(18,2),
    @payment_type   NVARCHAR(30) = N'Оплата аренды',
    @payment_method NVARCHAR(30) = N'Карта',
    @new_payment_id INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SET @new_payment_id = NULL;

    /* проверка 1: аренда должна существовать */
    IF NOT EXISTS (SELECT 1 FROM dbo.Rentals WHERE rental_id = @rental_id)
    BEGIN
        RAISERROR(N'Аренда с номером %d не найдена.', 16, 1, @rental_id);
        RETURN;
    END

    /* проверка 2: сумма должна быть больше нуля */
    IF @amount IS NULL OR @amount <= 0
    BEGIN
        RAISERROR(N'Сумма платежа должна быть больше нуля.', 16, 1);
        RETURN;
    END

    INSERT INTO dbo.Payments
        (rental_id, payment_date, amount, payment_type, payment_method, status)
    VALUES
        (@rental_id, CAST(GETDATE() AS DATE), @amount,
         @payment_type, @payment_method, N'Оплачен');

    SET @new_payment_id = SCOPE_IDENTITY();

    PRINT N'Платёж добавлен.';
END;
GO

/* --- ПРОВЕРКА 5.1. Корректный запуск -------------------------------- */
DECLARE @rid INT = (SELECT MIN(rental_id) FROM dbo.Rentals);
DECLARE @pid INT;

EXEC dbo.usp_AddPayment
     @rental_id      = @rid,
     @amount         = 4500.00,
     @new_payment_id = @pid OUTPUT;

SELECT @pid AS CreatedPaymentID;

SELECT payment_id, rental_id, payment_date, amount, payment_type, status
FROM   dbo.Payments
WHERE  payment_id = @pid;
GO

/* --- ПРОВЕРКА 5.2. Ошибочный запуск: аренды не существует ------------ */
/* Ожидается: Msg 50000 — «Аренда с номером 999999 не найдена.» */
DECLARE @pid2 INT;

EXEC dbo.usp_AddPayment
     @rental_id      = 999999,
     @amount         = 4500.00,
     @new_payment_id = @pid2 OUTPUT;

SELECT @pid2 AS CreatedPaymentID;    -- NULL: платёж не создан
GO

/* --- ПРОВЕРКА 5.3. Ошибочный запуск: сумма равна нулю ---------------- */
/* Ожидается: Msg 50000 — «Сумма платежа должна быть больше нуля.» */
DECLARE @rid3 INT = (SELECT MIN(rental_id) FROM dbo.Rentals);
DECLARE @pid3 INT;

EXEC dbo.usp_AddPayment
     @rental_id      = @rid3,
     @amount         = 0,
     @new_payment_id = @pid3 OUTPUT;

SELECT @pid3 AS CreatedPaymentID;    -- NULL: платёж не создан
GO


/* #####################################################################
   ЗАДАНИЕ 6. TRY...CATCH В ИЗМЕНЯЮЩЕЙ ПРОЦЕДУРЕ
   Проверки IF ловят только предвиденные ошибки. Ошибка может возникнуть
   и внутри самого INSERT — тогда нужны транзакция и TRY...CATCH.
   ##################################################################### */

IF OBJECT_ID('dbo.usp_AddPayment', 'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_AddPayment;
GO

CREATE PROCEDURE dbo.usp_AddPayment
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

/* --- ПРОВЕРКА 6.1. Корректный запуск -------------------------------- */
DECLARE @rid4 INT = (SELECT MIN(rental_id) FROM dbo.Rentals);
DECLARE @pid4 INT;

EXEC dbo.usp_AddPayment
     @rental_id      = @rid4,
     @amount         = 3200.00,
     @new_payment_id = @pid4 OUTPUT;

SELECT @pid4 AS CreatedPaymentID;
GO

/* --- ПРОВЕРКА 6.2. Те же ошибки, но обработанные CATCH --------------- */
/* Теперь вместо «красной» ошибки процедура возвращает таблицу
   с номером, текстом и строкой ошибки. */
DECLARE @pid5 INT;

EXEC dbo.usp_AddPayment
     @rental_id      = 999999,
     @amount         = 4500.00,
     @new_payment_id = @pid5 OUTPUT;
GO

/* --- ПРОВЕРКА 6.3. Ошибка внутри INSERT: сработает ROLLBACK ---------- */
/* Проверки пройдены, но сумма не помещается в DECIMAL(10,2).
   Ожидается: Msg 8115 «Arithmetic overflow...» из блока CATCH,
   а количество платежей до и после вызова совпадает. */
DECLARE @rid6 INT = (SELECT MIN(rental_id) FROM dbo.Rentals);
DECLARE @pid6 INT;
DECLARE @before INT = (SELECT COUNT(*) FROM dbo.Payments);

EXEC dbo.usp_AddPayment
     @rental_id      = @rid6,
     @amount         = 99999999999.00,
     @new_payment_id = @pid6 OUTPUT;

SELECT @before AS PaymentsBefore,
       (SELECT COUNT(*) FROM dbo.Payments) AS PaymentsAfter;
GO

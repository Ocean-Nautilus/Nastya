/* =====================================================================
   Лекция 5. Хранимые процедуры. Пункт 7 задания:
   для каждой процедуры минимум два запуска — корректный и ошибочный.

   Блоки выполнять по очереди (выделить блок -> F5) и сохранять
   результат из вкладок Results и Messages.
   ===================================================================== */

USE Archive_Muzi;
GO

/* =====================================================================
   1. usp_ShowAvailableEquipment (без параметров)
   ===================================================================== */

/* --- 1.1 КОРРЕКТНЫЙ ЗАПУСК: выводится список доступного оборудования */
EXEC dbo.usp_ShowAvailableEquipment;
GO

/* --- 1.2 ОШИБОЧНЫЙ ЗАПУСК: процедура параметров не принимает
   Ожидаемая ошибка:
   «Procedure or function usp_ShowAvailableEquipment has too many
    arguments specified.» */
EXEC dbo.usp_ShowAvailableEquipment @status = N'Доступно';
GO


/* =====================================================================
   2. usp_ShowRentalsByClient (один параметр)
   ===================================================================== */

/* Подсмотреть подходящего клиента (у кого есть аренды): */
SELECT TOP 5 client_id, COUNT(*) AS RentalsCount
FROM   dbo.Rentals
GROUP BY client_id
ORDER BY COUNT(*) DESC;
GO

/* --- 2.1 КОРРЕКТНЫЙ ЗАПУСК: аренды существующего клиента */
DECLARE @cid INT = (SELECT TOP 1 client_id
                    FROM dbo.Rentals
                    GROUP BY client_id
                    ORDER BY COUNT(*) DESC);

EXEC dbo.usp_ShowRentalsByClient @client_id = @cid;
GO

/* --- 2.2 ОШИБОЧНЫЙ ЗАПУСК: параметр не передан
   Ожидаемая ошибка:
   «Procedure or function 'usp_ShowRentalsByClient' expects parameter
    '@client_id', which was not supplied.» */
EXEC dbo.usp_ShowRentalsByClient;
GO

/* --- 2.3 ОШИБОЧНЫЙ ЗАПУСК: вместо числа передан текст
   Ожидаемая ошибка: «Conversion failed when converting the nvarchar
   value 'двадцать пять' to data type int.» */
EXEC dbo.usp_ShowRentalsByClient @client_id = N'двадцать пять';
GO


/* =====================================================================
   3–4. usp_AddClient (проверка дубля телефона + OUTPUT-параметр)
   ===================================================================== */

/* --- 3.1 КОРРЕКТНЫЙ ЗАПУСК: клиент создаётся, ID возвращается в OUTPUT */
DECLARE @id INT;

EXEC dbo.usp_AddClient
     @full_name     = N'Смирнова Анна Сергеевна',
     @phone         = N'+79990001122',
     @email         = N'anna.smirnova@example.com',
     @address       = N'г. Улан-Удэ, ул. Ленина, д. 12, кв. 5',
     @new_client_id = @id OUTPUT;

SELECT @id AS CreatedClientID;

/* убеждаемся, что строка действительно появилась */
SELECT client_id, full_name, phone, email, registration_date, status
FROM   dbo.Clients
WHERE  client_id = @id;
GO

/* --- 3.2 ОШИБОЧНЫЙ ЗАПУСК: тот же телефон второй раз
   Ожидается сообщение «Клиент с таким телефоном уже существует.
   Новая запись не создана.», а OUTPUT-параметр остаётся NULL. */
DECLARE @id2 INT;

EXEC dbo.usp_AddClient
     @full_name     = N'Дубликат Телефона Иванович',
     @phone         = N'+79990001122',
     @new_client_id = @id2 OUTPUT;

SELECT @id2 AS CreatedClientID;   -- NULL: клиент не создан
GO

/* --- 3.3 ОШИБОЧНЫЙ ЗАПУСК: пустое ФИО
   RAISERROR внутри BEGIN TRY сразу передаёт управление в BEGIN CATCH,
   поэтому вместо «красной» ошибки процедура аккуратно возвращает
   таблицу с номером и текстом ошибки. */
DECLARE @id3 INT;

EXEC dbo.usp_AddClient
     @full_name     = N'   ',
     @phone         = N'+79990009999',
     @new_client_id = @id3 OUTPUT;

SELECT @id3 AS CreatedClientID;   -- NULL: клиент не создан
GO


/* =====================================================================
   5. usp_AddPayment (аренда должна существовать, сумма > 0)
   ===================================================================== */

/* --- 5.1 КОРРЕКТНЫЙ ЗАПУСК: платёж по существующей аренде */
DECLARE @rid INT = (SELECT MIN(rental_id) FROM dbo.Rentals);
DECLARE @pid INT;

EXEC dbo.usp_AddPayment
     @rental_id      = @rid,
     @amount         = 4500.00,
     @payment_type   = N'Оплата аренды',
     @payment_method = N'Карта',
     @new_payment_id = @pid OUTPUT;

SELECT @pid AS CreatedPaymentID;

SELECT payment_id, rental_id, payment_date, amount, payment_type, status
FROM   dbo.Payments
WHERE  payment_id = @pid;
GO

/* --- 5.2 ОШИБОЧНЫЙ ЗАПУСК: аренды с таким номером нет
   Ожидается сообщение «Аренда с номером 999999 не найдена.» */
DECLARE @pid2 INT;

EXEC dbo.usp_AddPayment
     @rental_id      = 999999,
     @amount         = 4500.00,
     @new_payment_id = @pid2 OUTPUT;

SELECT @pid2 AS CreatedPaymentID;   -- NULL: платёж не создан
GO

/* --- 5.3 ОШИБОЧНЫЙ ЗАПУСК: сумма равна нулю
   Ожидается сообщение «Сумма платежа должна быть больше нуля.» */
DECLARE @rid3 INT = (SELECT MIN(rental_id) FROM dbo.Rentals);
DECLARE @pid3 INT;

EXEC dbo.usp_AddPayment
     @rental_id      = @rid3,
     @amount         = 0,
     @new_payment_id = @pid3 OUTPUT;

SELECT @pid3 AS CreatedPaymentID;   -- NULL: платёж не создан
GO

/* --- 5.4 ОШИБОЧНЫЙ ЗАПУСК: проверки пройдены, но ошибка возникает
   уже внутри INSERT — сумма не помещается в DECIMAL(10,2).
   Это и показывает работу TRY...CATCH: транзакция откатывается,
   а приложение получает текст ошибки
   «Arithmetic overflow error converting numeric to data type numeric.» */
DECLARE @rid4 INT = (SELECT MIN(rental_id) FROM dbo.Rentals);
DECLARE @pid4 INT;
DECLARE @before INT = (SELECT COUNT(*) FROM dbo.Payments);

EXEC dbo.usp_AddPayment
     @rental_id      = @rid4,
     @amount         = 99999999999.00,
     @new_payment_id = @pid4 OUTPUT;

/* количество платежей не изменилось — ROLLBACK сработал */
SELECT @before AS PaymentsBefore,
       (SELECT COUNT(*) FROM dbo.Payments) AS PaymentsAfter;
GO


/* =====================================================================
   6. usp_CloseRental (транзакция + TRY...CATCH)
   ===================================================================== */

/* --- 6.1 КОРРЕКТНЫЙ ЗАПУСК: закрываем активную аренду */
DECLARE @arid INT = (SELECT MIN(rental_id) FROM dbo.Rentals WHERE status = N'Активна');

SELECT rental_id, status, actual_end_date
FROM   dbo.Rentals WHERE rental_id = @arid;      -- до вызова

EXEC dbo.usp_CloseRental @rental_id = @arid;

SELECT rental_id, status, actual_end_date
FROM   dbo.Rentals WHERE rental_id = @arid;      -- после вызова
GO

/* --- 6.2 ОШИБОЧНЫЙ ЗАПУСК: аренда уже завершена
   Ожидается сообщение «Аренда с номером N уже завершена.» */
DECLARE @arid2 INT = (SELECT MIN(rental_id) FROM dbo.Rentals WHERE status = N'Завершена');

EXEC dbo.usp_CloseRental @rental_id = @arid2;
GO

/* --- 6.3 ОШИБОЧНЫЙ ЗАПУСК: аренды не существует */
EXEC dbo.usp_CloseRental @rental_id = 999999;
GO


/* =====================================================================
   Уборка тестовых данных (по желанию)
   ---------------------------------------------------------------------
   DELETE FROM dbo.Clients WHERE phone = N'+79990001122';
   ===================================================================== */

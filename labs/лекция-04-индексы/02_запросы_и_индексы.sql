/* =====================================================================
   Лекция 4. Индексы. Пункты 2–7 практического задания.
   База: Archive_Muzi

   Порядок работы в SSMS:
     1) включить Query -> Include Actual Execution Plan (Ctrl + M);
     2) выполнять блоки по очереди сверху вниз;
     3) после каждого запроса смотреть вкладку Messages (STATISTICS IO)
        и вкладку Execution plan.
   ===================================================================== */

USE Archive_Muzi;
GO

SET STATISTICS IO ON;
SET STATISTICS TIME ON;
GO

/* =====================================================================
   ПУНКТ 2–3. ТРИ ПОИСКОВЫХ ЗАПРОСА БЕЗ ДОПОЛНИТЕЛЬНЫХ ИНДЕКСОВ
   Для каждого сохраняем Actual Execution Plan и показатели STATISTICS IO.
   ===================================================================== */

/* --- Запрос 1. Поиск клиента по номеру телефона ---------------------- */
SELECT client_id, full_name, phone, email, status
FROM   dbo.Clients
WHERE  phone = N'+79001000057';
GO

/* --- Запрос 2. Поиск оборудования по бренду -------------------------- */
SELECT equipment_id, name, brand, model, rental_price, status
FROM   dbo.Equipment
WHERE  brand = N'Yamaha';
GO

/* --- Запрос 3. Платежи за июнь 2025 года ----------------------------- */
SELECT payment_id, rental_id, payment_date, amount, payment_method
FROM   dbo.Payments
WHERE  payment_date >= '2025-06-01'
  AND  payment_date <  '2025-07-01';
GO


/* =====================================================================
   ПУНКТ 4. КАКИЕ СТОЛБЦЫ СТОИТ ИНДЕКСИРОВАТЬ (объяснение — в отчёте)

   Запрос 1 -> Clients(phone):
        столбец стоит в WHERE с точным сравнением (=), значения почти
        уникальные, поэтому индекс вернёт 1 строку вместо просмотра
        всей таблицы.
   Запрос 2 -> Equipment(brand) + INCLUDE(name, model, rental_price, status):
        столбец в WHERE, результат — небольшая часть таблицы;
        включённые столбцы делают индекс покрывающим,
        и не понадобится Key Lookup.
   Запрос 3 -> Payments(payment_date) + INCLUDE(rental_id, amount, payment_method):
        поиск по диапазону дат, а индекс хранит значения отсортированными,
        значит сервер прочитает только нужный диапазон.

   Проверить, какие индексы уже есть:
   ===================================================================== */
SELECT  t.name  AS TableName,
        i.name  AS IndexName,
        i.type_desc,
        i.is_unique
FROM    sys.indexes i
JOIN    sys.tables  t ON t.object_id = i.object_id
WHERE   t.name IN (N'Clients', N'Equipment', N'Payments', N'Rentals')
ORDER BY t.name, i.index_id;
GO


/* =====================================================================
   ПУНКТ 5. СОЗДАНИЕ ИНДЕКСОВ
   ===================================================================== */

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Clients_Phone'
           AND object_id = OBJECT_ID('dbo.Clients'))
    DROP INDEX IX_Clients_Phone ON dbo.Clients;
GO
CREATE NONCLUSTERED INDEX IX_Clients_Phone
    ON dbo.Clients(phone)
    INCLUDE (full_name, email, status);
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Equipment_Brand'
           AND object_id = OBJECT_ID('dbo.Equipment'))
    DROP INDEX IX_Equipment_Brand ON dbo.Equipment;
GO
CREATE NONCLUSTERED INDEX IX_Equipment_Brand
    ON dbo.Equipment(brand)
    INCLUDE (name, model, rental_price, status);
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Payments_PaymentDate'
           AND object_id = OBJECT_ID('dbo.Payments'))
    DROP INDEX IX_Payments_PaymentDate ON dbo.Payments;
GO
CREATE NONCLUSTERED INDEX IX_Payments_PaymentDate
    ON dbo.Payments(payment_date)
    INCLUDE (rental_id, amount, payment_method);
GO


/* =====================================================================
   ПУНКТ 6. ТЕ ЖЕ САМЫЕ ЗАПРОСЫ ПОСЛЕ СОЗДАНИЯ ИНДЕКСОВ
   Снова сохраняем план и STATISTICS IO и сравниваем логические чтения.
   ===================================================================== */

/* --- Запрос 1 (после индекса) ---------------------------------------- */
SELECT client_id, full_name, phone, email, status
FROM   dbo.Clients
WHERE  phone = N'+79001000057';
GO

/* --- Запрос 2 (после индекса) ---------------------------------------- */
SELECT equipment_id, name, brand, model, rental_price, status
FROM   dbo.Equipment
WHERE  brand = N'Yamaha';
GO

/* --- Запрос 3 (после индекса) ---------------------------------------- */
SELECT payment_id, rental_id, payment_date, amount, payment_method
FROM   dbo.Payments
WHERE  payment_date >= '2025-06-01'
  AND  payment_date <  '2025-07-01';
GO


/* =====================================================================
   ПУНКТ 7. СОСТАВНОЙ ИНДЕКС ДЛЯ ЗАПРОСА С ДВУМЯ УСЛОВИЯМИ
   ===================================================================== */

/* Клиент, у которого больше всего аренд, — его номер удобно подставить
   в запрос ниже вместо 25. */
SELECT TOP 5 client_id, COUNT(*) AS RentalsCount
FROM   dbo.Rentals
GROUP BY client_id
ORDER BY COUNT(*) DESC;
GO

/* --- Запрос с двумя условиями ДО составного индекса ------------------ */
SELECT rental_id, client_id, rental_date, start_date, planned_end_date, status
FROM   dbo.Rentals
WHERE  client_id = 25
  AND  start_date >= '2025-06-01';
GO

/* --- Составной индекс ------------------------------------------------ */
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Rentals_Client_StartDate'
           AND object_id = OBJECT_ID('dbo.Rentals'))
    DROP INDEX IX_Rentals_Client_StartDate ON dbo.Rentals;
GO
/* client_id стоит первым, потому что по нему точное сравнение (=),
   а start_date вторым, потому что по нему диапазон (>=). */
CREATE NONCLUSTERED INDEX IX_Rentals_Client_StartDate
    ON dbo.Rentals(client_id, start_date)
    INCLUDE (rental_date, planned_end_date, status);
GO

/* --- Тот же запрос ПОСЛЕ составного индекса -------------------------- */
SELECT rental_id, client_id, rental_date, start_date, planned_end_date, status
FROM   dbo.Rentals
WHERE  client_id = 25
  AND  start_date >= '2025-06-01';
GO

SET STATISTICS IO OFF;
SET STATISTICS TIME OFF;
GO


/* =====================================================================
   Проверка созданных индексов
   ===================================================================== */
SELECT  t.name AS TableName,
        i.name AS IndexName,
        i.type_desc,
        STUFF((SELECT N', ' + c.name
               FROM sys.index_columns ic
               JOIN sys.columns c ON c.object_id = ic.object_id
                                 AND c.column_id = ic.column_id
               WHERE ic.object_id = i.object_id
                 AND ic.index_id  = i.index_id
                 AND ic.is_included_column = 0
               ORDER BY ic.key_ordinal
               FOR XML PATH('')), 1, 2, N'') AS KeyColumns
FROM    sys.indexes i
JOIN    sys.tables  t ON t.object_id = i.object_id
WHERE   i.name IN (N'IX_Clients_Phone', N'IX_Equipment_Brand',
                   N'IX_Payments_PaymentDate', N'IX_Rentals_Client_StartDate');
GO


/* =====================================================================
   При необходимости — удаление созданных индексов
   ---------------------------------------------------------------------
   DROP INDEX IX_Clients_Phone            ON dbo.Clients;
   DROP INDEX IX_Equipment_Brand          ON dbo.Equipment;
   DROP INDEX IX_Payments_PaymentDate     ON dbo.Payments;
   DROP INDEX IX_Rentals_Client_StartDate ON dbo.Rentals;
   ===================================================================== */

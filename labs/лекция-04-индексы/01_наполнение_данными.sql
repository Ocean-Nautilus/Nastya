/* =====================================================================
   Лекция 4. Индексы. Пункт 1 практического задания.
   Наполнение базы Archive_Muzi данными (значительно больше 200 строк,
   чтобы разница между Index Scan и Index Seek была заметна).

   Скрипт можно выполнять повторно: данные добавляются к уже имеющимся,
   существующие строки не удаляются.
   ===================================================================== */

USE Archive_Muzi;
GO

SET NOCOUNT ON;
GO

/* ---------------------------------------------------------------------
   0. Вспомогательная таблица чисел 1..10000
   --------------------------------------------------------------------- */
IF OBJECT_ID('tempdb..#Nums') IS NOT NULL
    DROP TABLE #Nums;

SELECT TOP (10000)
       n = ROW_NUMBER() OVER (ORDER BY (SELECT NULL))
INTO   #Nums
FROM   sys.all_objects a
CROSS JOIN sys.all_objects b;

CREATE UNIQUE CLUSTERED INDEX IX_Nums ON #Nums(n);
GO

/* ---------------------------------------------------------------------
   1. Категории оборудования (добавляются только недостающие)
   --------------------------------------------------------------------- */
INSERT INTO dbo.EquipmentCategories (name, description)
SELECT v.name, v.descr
FROM (VALUES
        (N'Гитары',            N'Электрические, акустические и бас-гитары'),
        (N'Клавишные',         N'Синтезаторы, MIDI-клавиатуры, цифровые пианино'),
        (N'Ударные',           N'Акустические и электронные барабанные установки'),
        (N'Микрофоны',         N'Динамические, конденсаторные, радиосистемы'),
        (N'Усилители',         N'Гитарные и басовые комбоусилители'),
        (N'Звуковые пульты',   N'Аналоговые и цифровые микшеры'),
        (N'Акустические системы', N'Активные и пассивные колонки, мониторы'),
        (N'Световое оборудование', N'Прожекторы, стробоскопы, дым-машины')
     ) AS v(name, descr)
WHERE NOT EXISTS (SELECT 1
                  FROM dbo.EquipmentCategories c
                  WHERE c.name = v.name);
GO

/* ---------------------------------------------------------------------
   2. Клиенты — 2000 строк
   --------------------------------------------------------------------- */
;WITH LN(id, v) AS (
    SELECT * FROM (VALUES
        (0,N'Иванов'),(1,N'Петров'),(2,N'Сидоров'),(3,N'Кузнецов'),(4,N'Смирнов'),
        (5,N'Попов'),(6,N'Васильев'),(7,N'Новиков'),(8,N'Морозов'),(9,N'Волков')) t(id,v)
),
FN(id, v) AS (
    SELECT * FROM (VALUES
        (0,N'Александр'),(1,N'Дмитрий'),(2,N'Максим'),(3,N'Сергей'),
        (4,N'Андрей'),(5,N'Егор'),(6,N'Никита')) t(id,v)
),
MN(id, v) AS (
    SELECT * FROM (VALUES
        (0,N'Сергеевич'),(1,N'Петрович'),(2,N'Иванович'),
        (3,N'Андреевич'),(4,N'Викторович')) t(id,v)
),
CT(id, v) AS (
    SELECT * FROM (VALUES
        (0,N'Москва'),(1,N'Санкт-Петербург'),(2,N'Казань'),
        (3,N'Новосибирск'),(4,N'Екатеринбург'),(5,N'Улан-Удэ')) t(id,v)
),
ST(id, v) AS (
    SELECT * FROM (VALUES
        (0,N'Ленина'),(1,N'Советская'),(2,N'Мира'),(3,N'Гагарина'),
        (4,N'Победы'),(5,N'Садовая'),(6,N'Школьная'),(7,N'Лесная')) t(id,v)
)
INSERT INTO dbo.Clients (full_name, phone, email, address, registration_date, status)
SELECT
    CONCAT(LN.v, N' ', FN.v, N' ', MN.v),
    CONCAT(N'+7900', RIGHT(CONCAT(N'0000000', CAST(1000000 + n.n AS NVARCHAR(10))), 7)),
    CONCAT(N'client', n.n, N'@example.com'),
    CONCAT(N'г. ', CT.v, N', ул. ', ST.v, N', д. ', (n.n % 80) + 1, N', кв. ', (n.n % 200) + 1),
    DATEADD(DAY, (n.n * 3) % 730, '2024-01-01'),
    CASE WHEN n.n % 10 = 0 THEN N'Заблокирован'
         WHEN n.n % 7  = 0 THEN N'Неактивен'
         ELSE N'Активен' END
FROM #Nums n
JOIN LN ON LN.id = n.n % 10
JOIN FN ON FN.id = n.n % 7
JOIN MN ON MN.id = n.n % 5
JOIN CT ON CT.id = n.n % 6
JOIN ST ON ST.id = n.n % 8
WHERE n.n <= 2000;
GO

/* ---------------------------------------------------------------------
   3. Оборудование — 500 строк
   --------------------------------------------------------------------- */
DECLARE @CatCount INT = (SELECT COUNT(*) FROM dbo.EquipmentCategories);
/* смещение для серийных/инвентарных номеров, чтобы они остались уникальными
   даже при повторном запуске скрипта */
DECLARE @Offset INT = (SELECT COUNT(*) FROM dbo.Equipment);

;WITH C AS (
    SELECT category_id,
           rn = ROW_NUMBER() OVER (ORDER BY category_id) - 1
    FROM dbo.EquipmentCategories
),
BR(id, v) AS (
    SELECT * FROM (VALUES
        (0,N'Yamaha'),(1,N'Fender'),(2,N'Roland'),(3,N'Shure'),
        (4,N'Behringer'),(5,N'Casio'),(6,N'Korg'),(7,N'Sennheiser')) t(id,v)
),
NM(id, v) AS (
    SELECT * FROM (VALUES
        (0,N'Электрогитара'),(1,N'Бас-гитара'),(2,N'Синтезатор'),
        (3,N'MIDI-клавиатура'),(4,N'Барабанная установка'),(5,N'Микрофон'),
        (6,N'Радиосистема'),(7,N'Комбоусилитель'),(8,N'Микшерный пульт'),
        (9,N'Активная колонка'),(10,N'Студийный монитор'),(11,N'Прожектор LED')) t(id,v)
)
INSERT INTO dbo.Equipment
    (category_id, name, brand, model, serial_number, inventory_number,
     rental_price, deposit, equipment_condition, status)
SELECT
    C.category_id,
    NM.v,
    BR.v,
    CONCAT(N'M-', 100 + (n.n % 400)),
    CONCAT(N'SN-GEN-', n.n + @Offset),
    CONCAT(N'INV-GEN-', n.n + @Offset),
    500.00 + (n.n % 30) * 100.00,
    2000.00 + (n.n % 20) * 500.00,
    CASE WHEN n.n % 5 = 0 THEN N'Отличное'
         WHEN n.n % 3 = 0 THEN N'Хорошее'
         ELSE N'Удовлетворительное' END,
    CASE WHEN n.n % 11 = 0 THEN N'На обслуживании'
         WHEN n.n % 4  = 0 THEN N'Арендовано'
         ELSE N'Доступно' END
FROM #Nums n
JOIN C  ON C.rn  = n.n % @CatCount
JOIN BR ON BR.id = n.n % 8
JOIN NM ON NM.id = n.n % 12
WHERE n.n <= 500;
GO

/* ---------------------------------------------------------------------
   4. Аренды — 5000 строк
   --------------------------------------------------------------------- */
DECLARE @ClientCount INT = (SELECT COUNT(*) FROM dbo.Clients);

;WITH C AS (
    SELECT client_id,
           rn = ROW_NUMBER() OVER (ORDER BY client_id) - 1
    FROM dbo.Clients
)
INSERT INTO dbo.Rentals
    (client_id, rental_date, start_date, planned_end_date,
     actual_end_date, status, rental_comment)
SELECT
    C.client_id,
    DATEADD(DAY, (n.n * 2) % 600, '2025-01-01'),
    DATEADD(DAY, (n.n * 2) % 600, '2025-01-01'),
    DATEADD(DAY, ((n.n * 2) % 600) + 3 + (n.n % 12), '2025-01-01'),
    CASE WHEN n.n % 3 = 0
         THEN DATEADD(DAY, ((n.n * 2) % 600) + 3 + (n.n % 12), '2025-01-01')
         ELSE NULL END,
    CASE WHEN n.n % 3 = 0 THEN N'Завершена'
         WHEN n.n % 9 = 0 THEN N'Просрочена'
         ELSE N'Активна' END,
    CASE WHEN n.n % 25 = 0 THEN N'Аренда на выездное мероприятие' ELSE NULL END
FROM #Nums n
JOIN C ON C.rn = n.n % @ClientCount
WHERE n.n <= 5000;
GO

/* ---------------------------------------------------------------------
   5. Позиции аренды — 8000 строк
   --------------------------------------------------------------------- */
DECLARE @RentalCount INT = (SELECT COUNT(*) FROM dbo.Rentals);
DECLARE @EquipCount  INT = (SELECT COUNT(*) FROM dbo.Equipment);

;WITH R AS (
    SELECT rental_id,
           rn = ROW_NUMBER() OVER (ORDER BY rental_id) - 1
    FROM dbo.Rentals
),
E AS (
    SELECT equipment_id, rental_price,
           rn = ROW_NUMBER() OVER (ORDER BY equipment_id) - 1
    FROM dbo.Equipment
)
INSERT INTO dbo.RentalItems
    (rental_id, equipment_id, price_per_day, quantity,
     rental_days, condition_before, condition_after)
SELECT
    R.rental_id,
    E.equipment_id,
    E.rental_price,
    1 + (n.n % 3),
    1 + (n.n % 14),
    CASE WHEN n.n % 4 = 0 THEN N'Отличное' ELSE N'Хорошее' END,
    CASE WHEN n.n % 6 = 0 THEN N'Требует чистки' ELSE N'Хорошее' END
FROM #Nums n
JOIN R ON R.rn = n.n % @RentalCount
JOIN E ON E.rn = (n.n * 7) % @EquipCount
WHERE n.n <= 8000;
GO

/* ---------------------------------------------------------------------
   6. Платежи — 6000 строк
   --------------------------------------------------------------------- */
DECLARE @RentalCount2 INT = (SELECT COUNT(*) FROM dbo.Rentals);

;WITH R AS (
    SELECT rental_id, rental_date,
           rn = ROW_NUMBER() OVER (ORDER BY rental_id) - 1
    FROM dbo.Rentals
)
INSERT INTO dbo.Payments
    (rental_id, payment_date, amount, payment_type, payment_method, status)
SELECT
    R.rental_id,
    DATEADD(DAY, n.n % 5, R.rental_date),
    1500.00 + (n.n % 40) * 250.00,
    CASE WHEN n.n % 3 = 0 THEN N'Залог'
         WHEN n.n % 7 = 0 THEN N'Штраф'
         ELSE N'Оплата аренды' END,
    CASE WHEN n.n % 2 = 0 THEN N'Карта'
         WHEN n.n % 5 = 0 THEN N'Перевод'
         ELSE N'Наличные' END,
    CASE WHEN n.n % 13 = 0 THEN N'Ожидает' ELSE N'Оплачен' END
FROM #Nums n
JOIN R ON R.rn = n.n % @RentalCount2
WHERE n.n <= 6000;
GO

/* ---------------------------------------------------------------------
   7. Обслуживание — 1000 строк
   --------------------------------------------------------------------- */
DECLARE @EquipCount2 INT = (SELECT COUNT(*) FROM dbo.Equipment);

;WITH E AS (
    SELECT equipment_id,
           rn = ROW_NUMBER() OVER (ORDER BY equipment_id) - 1
    FROM dbo.Equipment
)
INSERT INTO dbo.Maintenance
    (equipment_id, service_date, service_type, description, cost,
     condition_before, condition_after, next_service_date, status)
SELECT
    E.equipment_id,
    DATEADD(DAY, (n.n * 3) % 600, '2025-01-01'),
    CASE WHEN n.n % 4 = 0 THEN N'Плановое ТО'
         WHEN n.n % 3 = 0 THEN N'Замена струн'
         WHEN n.n % 5 = 0 THEN N'Ремонт разъёма'
         ELSE N'Чистка и настройка' END,
    N'Работы выполнены сервисным инженером мастерской',
    300.00 + (n.n % 25) * 120.00,
    CASE WHEN n.n % 3 = 0 THEN N'Удовлетворительное' ELSE N'Хорошее' END,
    N'Отличное',
    DATEADD(DAY, ((n.n * 3) % 600) + 180, '2025-01-01'),
    CASE WHEN n.n % 8 = 0 THEN N'В работе' ELSE N'Завершено' END
FROM #Nums n
JOIN E ON E.rn = n.n % @EquipCount2
WHERE n.n <= 1000;
GO

/* ---------------------------------------------------------------------
   8. Проверка: сколько строк получилось в каждой таблице
   --------------------------------------------------------------------- */
SELECT N'Clients'             AS TableName, COUNT(*) AS RowsCount FROM dbo.Clients
UNION ALL SELECT N'EquipmentCategories', COUNT(*) FROM dbo.EquipmentCategories
UNION ALL SELECT N'Equipment',           COUNT(*) FROM dbo.Equipment
UNION ALL SELECT N'Rentals',             COUNT(*) FROM dbo.Rentals
UNION ALL SELECT N'RentalItems',         COUNT(*) FROM dbo.RentalItems
UNION ALL SELECT N'Payments',            COUNT(*) FROM dbo.Payments
UNION ALL SELECT N'Maintenance',         COUNT(*) FROM dbo.Maintenance;
GO

DROP TABLE #Nums;
GO

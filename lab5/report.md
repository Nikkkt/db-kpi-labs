# Лабораторна робота №5

**Тема:** Робота з базовими функціями граф-орієнтованої БД Neo4j

---

## 1. Мета роботи
Набути практичних навичок роботи з граф-орієнтованою базою даних Neo4j, освоїти мову запитів Cypher, реалізувати модель предметної області онлайн-магазину та написати запити для аналізу даних.

## 2. Модель даних
Предметна область — онлайн-магазин. Граф складається з таких вузлів та зв'язків:
* **Item** (id, name, price) — товар
* **Customer** (id, name) — покупець
* **Order** (id, date) — замовлення
* **Customer -[:bought]-> Order** — покупець зробив замовлення
* **Order -[:contains]-> Item** — замовлення містить товар
* **Customer -[:view]-> Item** — покупець переглянув товар

---

## 3. Наповнення бази даних
Нижче наведені Cypher-команди для створення вузлів та зв'язків у Neo4j Sandbox / локальній інсталяції:

```cypher
// === ОЧИСТКА БАЗИ (при повторному запуску) === //
MATCH (n) DETACH DELETE n; //

// === СТВОРЕННЯ ВУЗЛІВ Items === //
CREATE (i1:Item {id: 1, name: 'Ноутбук Dell XPS 15', price: 45000}), //
(i2:Item {id: 2, name: 'Механічна клавіатура Keychron K2', price: 3200}), //
(i3:Item {id: 3, name: 'Монітор LG UltraWide 29"', price: 18000}), //
(i4:Item {id: 4, name: 'Навушники Sony WH-1000XM5', price: 12000}), //
(i5:Item {id: 5, name: 'Веб-камера Logitech C920', price: 3500}), //
(i6:Item {id: 6, name: 'USB-хаб Anker 7-port', price: 1800}); //

// === СТВОРЕННЯ ВУЗЛІВ Customers === //
CREATE (c1:Customer {id: 1, name: 'Олена Коваленко'}), //
(c2:Customer {id: 2, name: 'Микола Шевченко'}), //
(c3:Customer {id: 3, name: 'Софія Бондаренко'}); //

// === СТВОРЕННЯ ВУЗЛІВ Orders === //
CREATE (o1:Order {id: 1, date: '2024-01-10'}), //
(o2:Order {id: 2, date: '2024-01-15'}), //
(o3:Order {id: 3, date: '2024-02-01'}), //
(o4:Order {id: 4, date: '2024-02-20'}), //
(o5:Order {id: 5, date: '2024-03-05'}); //

// === ЗʼЯЗКИ Customer -[bought]-> Order === //
MATCH (c1:Customer {id:1}),(c2:Customer {id:2}),(c3:Customer {id:3}), //
(o1:Order {id:1}),(o2:Order {id:2}),(o3:Order {id:3}), //
(o4:Order {id:4}),(o5:Order {id:5}) //
CREATE (c1)-[:bought]->(o1), //
(c1)-[:bought]->(o2), //
(c2)-[:bought]->(o3), //
(c2)-[:bought]->(o4), //
(c3)-[:bought]->(o5); //

// === ЗʼЯЗКИ Order -[contains]-> Item === //
MATCH (o1:Order {id:1}),(o2:Order {id:2}),(o3:Order {id:3}), //
(o4:Order {id:4}),(o5:Order {id:5}), //
(i1:Item {id:1}),(i2:Item {id:2}),(i3:Item {id:3}), //
(i4:Item {id:4}),(i5:Item {id:5}),(i6:Item {id:6}) //
CREATE (o1)-[:contains]->(i1), //
(o1)-[:contains]->(i2), //
(o2)-[:contains]->(i3), //
(o2)-[:contains]->(i4), //
(o3)-[:contains]->(i2), //
(o3)-[:contains]->(i5), //
(o4)-[:contains]->(i1), //
(o4)-[:contains]->(i6), //
(o5)-[:contains]->(i3), //
(o5)-[:contains]->(i4), //
(o5)-[:contains]->(i5); //

// === ЗʼЯЗКИ Customer -[view]-> Item === //
MATCH (c1:Customer {id:1}),(c2:Customer {id:2}),(c3:Customer {id:3}), //
(i3:Item {id:3}),(i5:Item {id:5}),(i6:Item {id:6}),(i4:Item {id:4}) //
CREATE (c1)-[:view]->(i3), //
(c1)-[:view]->(i5), //
(c2)-[:view]->(i6), //
(c3)-[:view]->(i4); //
```

---

## 4. Запити та результати виконання

### 1. Items що входять в конкретний Order (Order id = 1)
**Запит (Cypher):**
```cypher
MATCH (o:Order {id: 1})-[:contains]->(i:Item) //
RETURN i.id AS item_id, i.name AS name, i.price AS price; //
```
**Результат виконання:**

| item_id | name | price |
| :--- | :--- | :--- |
| 1 | Ноутбук Dell XPS 15 | 45000 |
| 2 | Механічна клавіатура Keychron K2 | 3200 |

### 2. Вартість конкретного Order (Order id = 2)
**Запит (Cypher):**
` ` `cypher
MATCH (o:Order {id: 2})-[:contains]->(i:Item) //
RETURN o.id AS order_id, o.date AS date, SUM(i.price) AS total_price; //
` ` `
**Результат виконання:**

| order_id | date | total_price |
| :--- | :--- | :--- |
| 2 | 2024-01-15 | 30000 |

### 3. Всі Orders конкретного Customer (Customer id = 1)
**Запит (Cypher):**
` ` `cypher
MATCH (c:Customer {id: 1})-[:bought]->(o:Order) //
RETURN o.id AS order_id, o.date AS date; //
` ` `
**Результат виконання:**

| order_id | date |
| :--- | :--- |
| 1 | 2024-01-10 |
| 2 | 2024-01-15 |

### 4. Всі Items куплені конкретним Customer (Customer id = 1)
**Запит (Cypher):**
` ` `cypher
MATCH (c:Customer {id: 1})-[:bought]->(o:Order)-[:contains]->(i:Item) //
RETURN DISTINCT i.id AS item_id, i.name AS name, i.price AS price; //
` ` `
**Результат виконання:**

| item_id | name | price |
| :--- | :--- | :--- |
| 1 | Ноутбук Dell XPS 15 | 45000 |
| 2 | Механічна клавіатура Keychron K2 | 3200 |
| 3 | Монітор LG UltraWide 29" | 18000 |
| 4 | Навушники Sony WH-1000XM5 | 12000 |

### 5. Загальна кількість Items куплених Customer id = 1
**Запит (Cypher):**
` ` `cypher
MATCH (c:Customer {id: 1})-[:bought]->(o:Order)-[:contains]->(i:Item) //
RETURN c.name AS customer, COUNT(i) AS total_items; //
` ` `
**Результат виконання:**

| customer | total_items |
| :--- | :--- |
| Олена Коваленко | 4 |

### 6. Загальна сума покупок Customer id = 1
**Запит (Cypher):**
` ` `cypher
MATCH (c:Customer {id: 1})-[:bought]->(o:Order)-[:contains]->(i:Item) //
RETURN c.name AS customer, SUM(i.price) AS total_spent; //
` ` `
**Результат виконання:**

| customer | total_spent |
| :--- | :--- |
| Олена Коваленко | 78200 |

### 7. Кількість покупок кожного товару (з сортуванням)
**Запит (Cypher):**
` ` `cypher
MATCH (o:Order)-[:contains]->(i:Item) //
RETURN i.name AS item_name, COUNT(o) AS purchase_count //
ORDER BY purchase_count DESC; //
` ` `
**Результат виконання:**

| item_name | purchase_count |
| :--- | :--- |
| Монітор LG UltraWide 29" | 2 |
| Навушники Sony WH-1000XM5 | 2 |
| Ноутбук Dell XPS 15 | 2 |
| Механічна клавіатура Keychron K2 | 2 |
| Веб-камера Logitech C920 | 2 |
| USB-хаб Anker 7-port | 1 |

### 8. Всі Items переглянуті Customer id = 1
**Запит (Cypher):**
` ` `cypher
MATCH (c:Customer {id: 1})-[:view]->(i:Item) //
RETURN i.id AS item_id, i.name AS name, i.price AS price; //
` ` `
**Результат виконання:**

| item_id | name | price |
| :--- | :--- | :--- |
| 3 | Монітор LG UltraWide 29" | 18000 |
| 5 | Веб-камера Logitech C920 | 3500 |

### 9. Items куплені разом з Item id = 2
**Запит (Cypher):**
` ` `cypher
MATCH (i:Item {id: 2})<-[:contains]-(o:Order)-[:contains]->(other:Item) //
WHERE other.id <> 2 //
RETURN DISTINCT other.id AS item_id, other.name AS name, other.price AS price; //
` ` `
**Результат виконання:**

| item_id | name | price |
| :--- | :--- | :--- |
| 1 | Ноутбук Dell XPS 15 | 45000 |
| 5 | Веб-камера Logitech C920 | 3500 |

### 10. Customers що купили Item id = 1
**Запит (Cypher):**
` ` `cypher
MATCH (c:Customer)-[:bought]->(o:Order)-[:contains]->(i:Item {id: 1}) //
RETURN DISTINCT c.id AS customer_id, c.name AS customer_name; //
` ` `
**Результат виконання:**

| customer_id | customer_name |
| :--- | :--- |
| 1 | Олена Коваленко |
| 2 | Микола Шевченко |

### 11. Товари переглянуті але не куплені Customer id = 1
**Запит (Cypher):**
` ` `cypher
MATCH (c:Customer {id: 1})-[:view]->(i:Item) //
WHERE NOT (c)-[:bought]->(:Order)-[:contains]->(i) //
RETURN i.id AS item_id, i.name AS name, i.price AS price; //
` ` `
**Результат виконання:**

| item_id | name | price |
| :--- | :--- | :--- |
| 5 | Веб-камера Logitech C920 | 3500 |

---

## 5. Висновки
* В ході виконання лабораторної роботи було успішно опановано роботу з граф-орієнтованою СУБД Neo4j та мовою запитів Cypher.
* Змодельовано предметну область онлайн-магазину з вузлами Item, Customer, Order та зв'язками bought, contains, view.
* Реалізовано 11 запитів різних типів: пошук товарів за замовленням, підрахунок вартості, аналіз покупок клієнтів, визначення популярності товарів, виявлення товарів переглянутих але не куплених, а також пошук супутніх товарів.
* Граф-орієнтована модель дозволяє природно відображати складні зв'язки між сутностями та ефективно їх аналізувати за допомогою виразного синтаксису Cypher.

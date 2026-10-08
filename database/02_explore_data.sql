/* =====================================================================
   OneMoi — queries to explore the demo data (PostgreSQL, database "onemoi")
   pgAdmin: right-click database onemoi → Query Tool → paste → select one
   query → F5. All names are lower_snake_case, so no quotes are needed.
   ===================================================================== */

-- 1. All tables by schema, with row counts
SELECT schemaname || '.' || relname AS table_name, n_live_tup AS rows
FROM pg_stat_user_tables
ORDER BY schemaname, relname;

-- 2. Every login and what kind it is  (user_type: 1 SuperAdmin, 2 TenantUser, 3 Individual)
SELECT u.id, u.full_name, u.full_name_ta, u.mobile, u.email, u.user_type, u.is_mobile_verified, u.last_login_at,
       t.name AS vendor, m.role AS vendor_role                -- role: 1 Owner, 2 Manager, 3 Accountant
FROM auth.users u
LEFT JOIN core.tenant_members m ON m.user_id = u.id
LEFT JOIN core.tenants t ON t.id = m.tenant_id
ORDER BY u.user_type, u.id;

-- 3. Vendors with their branding  (status: 1 Pending, 2 Active, 3 Suspended)
SELECT id, code, name, name_ta, owner_name, mobile, email, city, logo_path, receipt_header, receipt_footer, status
FROM core.tenants;

-- 4. THE CORE IDEA — Ram (9876543210) sees Moi given through EVERY vendor
SELECT p.mobile, e.initial || ' ' || e.name AS name_as_written, e.name_ta,
       f.name AS function_name, f.name_ta AS function_name_ta, t.name AS vendor, e.amount, e.payment_mode, e.receipt_no
FROM moi.persons p
JOIN moi.moi_entries e ON e.person_id = p.id AND e.status = 1 AND NOT e.is_deleted
JOIN evt.functions f ON f.id = e.function_id
JOIN core.tenants t ON t.id = e.tenant_id
WHERE p.mobile = '9876543210'
ORDER BY f.function_date DESC;

SELECT SUM(e.amount) AS ram_total
FROM moi.moi_entries e JOIN moi.persons p ON p.id = e.person_id
WHERE p.mobile = '9876543210' AND e.status = 1;

-- 5. Functions with English + Tamil names, owner and totals
--    (status: 1 Draft, 2 Scheduled, 3 Live, 4 Closed, 5 Cancelled)
SELECT f.id, f.code, t.code AS vendor, f.name, f.name_ta, f.owner_name, f.owner_name_ta, f.owner_mobile,
       f.location, f.location_ta, f.function_date, f.start_time, f.end_time, f.status,
       (SELECT COUNT(*) FROM moi.moi_entries e WHERE e.function_id = f.id AND e.status = 1) AS entries,
       (SELECT SUM(amount) FROM moi.moi_entries e WHERE e.function_id = f.id AND e.status = 1) AS collected
FROM evt.functions f JOIN core.tenants t ON t.id = f.tenant_id
ORDER BY f.function_date DESC;

-- 6. 3 operators × 3 functions on the same day — who works where
SELECT f.name AS function_name, c.name AS counter, o.code AS operator_id, o.name AS operator, a.valid_from, a.valid_to, a.is_active
FROM evt.operator_assignments a
JOIN evt.functions f ON f.id = a.function_id
JOIN evt.counters c ON c.id = a.counter_id
JOIN core.operators o ON o.id = a.operator_id
ORDER BY f.name, c.number;

-- 7. Moi entries of one function: initial, spouse, Tamil names, highlighted category
SELECT e.serial_no, e.receipt_no, e.initial, e.name, e.name_ta, e.spouse_initial, e.spouse_name, e.spouse_name_ta,
       e.work, e.city, e.city_ta, mc.name AS category, mc.name_ta AS category_ta, e.is_highlighted, e.amount, e.payment_mode,
       o.name AS operator
FROM moi.moi_entries e
LEFT JOIN master.moi_categories mc ON mc.id = e.moi_category_id
LEFT JOIN core.operators o ON o.id = e.operator_id
WHERE e.function_id = (SELECT id FROM evt.functions WHERE code = 'MKV7Q2')
ORDER BY e.serial_no;

-- 8. Denominations: 500 × 150 + 100 × 250 = 1,00,000
SELECT e.receipt_no, e.name, e.amount, d.note_value, d.count, d.total
FROM moi.moi_entry_denominations d JOIN moi.moi_entries e ON e.id = d.moi_entry_id
ORDER BY e.id, d.note_value DESC;

-- 9. Gifts given with Moi
SELECT e.receipt_no, e.name, gt.name AS gift_type, g.description, g.description_ta, g.quantity, g.estimated_value
FROM moi.moi_entry_gifts g JOIN moi.moi_entries e ON e.id = g.moi_entry_id
LEFT JOIN master.gift_item_types gt ON gt.id = g.gift_item_type_id;

-- 10. Money taken out during the function, and cash in hand per function
SELECT f.name, x.taken_by_name, x.taken_by_name_ta, x.relation, x.purpose, x.amount, x.entry_at
FROM moi.function_expenses x JOIN evt.functions f ON f.id = x.function_id
WHERE NOT x.is_deleted;

SELECT f.name,
       SUM(e.amount) FILTER (WHERE e.payment_mode = 1) AS cash_collected,
       (SELECT COALESCE(SUM(amount), 0) FROM moi.function_expenses x
         WHERE x.function_id = f.id AND NOT x.is_deleted AND x.payment_mode = 1) AS cash_expenses
FROM evt.functions f JOIN moi.moi_entries e ON e.function_id = f.id AND e.status = 1
GROUP BY f.id, f.name;

-- 11. Same name in the same city — why the INITIAL matters
SELECT name, city, initial, spouse_name, mobile FROM moi.moi_entries
WHERE lower(name) = 'ram' AND lower(city) = 'pollachi';

-- 12. OTP requests (only a HASH of the code is stored) and the messages sent (OTP masked)
SELECT id, channel, destination, purpose, code_hash, created_at, expires_at, attempts, is_used
FROM auth.otp_requests ORDER BY id DESC LIMIT 20;
SELECT id, channel, recipient, body, status, created_at FROM auth.notification_logs ORDER BY id DESC LIMIT 20;

-- 13. Audit trail: logins, reversals, approvals
SELECT id, action, tenant_id, principal_type, principal_id, details, ip_address, created_at
FROM auth.audit_logs ORDER BY id DESC LIMIT 50;

-- 14. Masters: system rows (tenant_id NULL) vs a vendor's own rows
SELECT 'function_type' AS master, id, tenant_id, name, name_ta FROM master.function_types
UNION ALL SELECT 'moi_category', id, tenant_id, name, name_ta FROM master.moi_categories
UNION ALL SELECT 'gift_item_type', id, tenant_id, name, name_ta FROM master.gift_item_types
UNION ALL SELECT 'expense_category', id, tenant_id, name, name_ta FROM master.expense_categories;

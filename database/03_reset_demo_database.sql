/* =====================================================================
   Reset the LOCAL demo database (PostgreSQL "onemoi"): drops all OneMoi tables.
   Next time the API starts it re-creates the tables and loads fresh demo data
   (with today's date, so the dashboard and operator logins work again).
   ⚠ Deletes ALL data in database onemoi. Use only on your development machine.
   ===================================================================== */
DROP SCHEMA IF EXISTS auth, core, master, evt, moi CASCADE;
DROP TABLE IF EXISTS public."__EFMigrationsHistory";
-- Now start the API: tables + demo data are created automatically.

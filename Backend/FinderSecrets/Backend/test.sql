-- @db findersecret
SELECT version();
SELECT current_database();
SELECT table_name FROM information_schema.tables WHERE table_schema = 'public';
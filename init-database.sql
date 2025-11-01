-- Автоматическое создание таблиц при первом запуске
CREATE TABLE IF NOT EXISTS users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(100) NOT NULL UNIQUE,
    email VARCHAR(255) NOT NULL UNIQUE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    last_login TIMESTAMP WITH TIME ZONE
);

CREATE TABLE IF NOT EXISTS scan_requests (
    id SERIAL PRIMARY KEY,
    user_id INTEGER REFERENCES users(id),
    input_data TEXT NOT NULL,
    input_type VARCHAR(50) NOT NULL,
    scan_duration INTEGER DEFAULT 0,
    secrets_count INTEGER DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS found_secrets (
    id SERIAL PRIMARY KEY,
    request_id INTEGER REFERENCES scan_requests(id),
    secret_type VARCHAR(100) NOT NULL,
    secret_value TEXT NOT NULL,
    variable_name VARCHAR(200),
    line_number INTEGER,
    position INTEGER,
    first_found_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    last_found_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    is_active BOOLEAN DEFAULT true
);

CREATE TABLE IF NOT EXISTS scan_history (
    id SERIAL PRIMARY KEY,
    user_id INTEGER REFERENCES users(id),
    request_id INTEGER REFERENCES scan_requests(id),
    input_type VARCHAR(50),
    input_preview TEXT,
    secrets_found INTEGER DEFAULT 0,
    scanned_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

INSERT INTO users (username, email, created_at) 
VALUES ('default', 'default@example.com', CURRENT_TIMESTAMP)
ON CONFLICT (username) DO NOTHING;
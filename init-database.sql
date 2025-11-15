DROP TABLE IF EXISTS scan_history;
DROP TABLE IF EXISTS found_secrets;
DROP TABLE IF EXISTS scan_requests;
DROP TABLE IF EXISTS users;

CREATE TABLE IF NOT EXISTS users (
    id UUID PRIMARY KEY,
    username VARCHAR(100) NOT NULL UNIQUE,
    email VARCHAR(255) NOT NULL UNIQUE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    password VARCHAR(255),
    roles VARCHAR(100)
);

CREATE TABLE IF NOT EXISTS scan_requests (
    id SERIAL PRIMARY KEY,
    user_id UUID REFERENCES users(id) ON DELETE CASCADE,
    input_data TEXT NOT NULL,
    input_type VARCHAR(50) NOT NULL,
    scan_duration INTEGER DEFAULT 0,
    secrets_count INTEGER DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS found_secrets (
    id SERIAL PRIMARY KEY,
    request_id INTEGER REFERENCES scan_requests(id) ON DELETE CASCADE,
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
    user_id UUID REFERENCES users(id) ON DELETE CASCADE,
    request_id INTEGER REFERENCES scan_requests(id) ON DELETE CASCADE,
    input_type VARCHAR(50),
    input_preview TEXT,
    secrets_found INTEGER DEFAULT 0,
    scanned_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

INSERT INTO users (id, username, email, created_at) 
VALUES ('11111111-1111-1111-1111-111111111111', 'default', 'default@example.com', CURRENT_TIMESTAMP)
ON CONFLICT (username) DO NOTHING;

CREATE INDEX IF NOT EXISTS idx_found_secrets_type ON found_secrets(secret_type);
CREATE INDEX IF NOT EXISTS idx_found_secrets_found_date ON found_secrets(first_found_at);
CREATE INDEX IF NOT EXISTS idx_found_secrets_request_id ON found_secrets(request_id);
CREATE INDEX IF NOT EXISTS idx_scan_requests_user_id ON scan_requests(user_id);
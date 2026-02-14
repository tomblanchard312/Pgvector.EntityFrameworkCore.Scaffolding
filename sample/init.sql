-- Enable pgvector extension
CREATE EXTENSION IF NOT EXISTS vector;

-- Sample products table with vector embedding
CREATE TABLE products (
    id SERIAL PRIMARY KEY,
    name VARCHAR(200) NOT NULL,
    category VARCHAR(100),
    price DECIMAL(10, 2) NOT NULL DEFAULT 0,
    embedding vector(3)
);

-- Sample documents table with vector
CREATE TABLE documents (
    id SERIAL PRIMARY KEY,
    title VARCHAR(500) NOT NULL,
    content TEXT,
    embedding vector(3)
);

-- Insert sample products with 3D embeddings (for testing)
INSERT INTO products (name, category, price, embedding) VALUES
    ('Wireless Headphones', 'Electronics', 99.99, '[1, 1, 1]'),
    ('Bluetooth Speaker', 'Electronics', 49.99, '[2, 2, 2]'),
    ('USB-C Cable', 'Electronics', 12.99, '[1, 1, 2]'),
    ('Desk Lamp', 'Home', 34.99, '[0.5, 0.5, 0.5]'),
    ('Office Chair', 'Furniture', 199.99, '[3, 3, 3]');

-- Insert sample documents
INSERT INTO documents (title, content, embedding) VALUES
    ('Getting Started', 'Introduction to the system.', '[1, 0, 0]'),
    ('Advanced Guide', 'Deep dive into features.', '[0, 1, 0]'),
    ('Troubleshooting', 'Common issues and fixes.', '[0, 0, 1]');

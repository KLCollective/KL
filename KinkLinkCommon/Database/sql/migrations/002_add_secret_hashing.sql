-- Add secret hashing columns to Users table for security
-- This migration adds columns to store hashed secrets and salts

-- Add indexes for performance on authentication queries
CREATE INDEX IF NOT EXISTS idx_users_secret_hash ON Users(secret_key_hash);

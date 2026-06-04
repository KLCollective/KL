-- Change lock_id from VARCHAR(24) to INTEGER to use LockKind enum values
-- The Locks table primary key constraint will be recreated after the column type change

-- Drop the primary key constraint (PostgreSQL auto-names it locks_pkey)
ALTER TABLE Locks DROP CONSTRAINT locks_pkey;

-- Drop indexes referencing lock_id (they will be recreated)
DROP INDEX IF EXISTS idx_locks_lockee_id;

-- Alter the column type from VARCHAR(24) to INTEGER
-- NOTE: This migration requires that it be truncated prior to altering as even a single existing lock will
-- cause the migration to fail (strings like "12345678" can't change to integers)
TRUNCATE Locks;
ALTER TABLE Locks ALTER COLUMN lock_id TYPE INTEGER USING lock_id::INTEGER;

-- Recreate the primary key constraint
ALTER TABLE Locks ADD PRIMARY KEY (lock_id, lockee_id);

-- Recreate dropped indexes
CREATE INDEX IF NOT EXISTS idx_locks_lockee_id ON Locks(lockee_id);

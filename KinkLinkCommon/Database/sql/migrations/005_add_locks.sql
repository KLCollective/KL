CREATE TABLE IF NOT EXISTS Locks (
    lock_id VARCHAR(24) NOT NULL,
    lockee_id INTEGER NOT NULL REFERENCES Users(id) ON DELETE CASCADE,
    locker_id INTEGER NOT NULL REFERENCES Users(id) ON DELETE CASCADE,
    lock_priority INTEGER NOT NULL DEFAULT 0,
    can_self_unlock BOOLEAN NOT NULL DEFAULT false,
    expires TIMESTAMP WITH TIME ZONE,
    password VARCHAR(256),
    PRIMARY KEY (lock_id, lockee_id)
);

CREATE INDEX IF NOT EXISTS idx_locks_lockee_id ON Locks(lockee_id);
CREATE INDEX IF NOT EXISTS idx_locks_locker_id ON Locks(locker_id);
CREATE INDEX IF NOT EXISTS idx_locks_expires ON Locks(expires) WHERE expires IS NOT NULL;

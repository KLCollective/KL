-- name: GetLocksForLockee :many
-- Retrieves all locks for a specific user (lockee) and gets the alias of the locker by locker_id
SELECT l.lock_id, l.lockee_id, l.locker_id, l.lock_priority, l.can_self_unlock, l.expires, l.password,
       pLocker.alias as locker_alias
FROM Locks l
JOIN Profiles pLocker ON l.locker_id = pLocker.id
WHERE l.lockee_id = $1;

-- name: GetLockById :one
-- Retrieves a specific lock by lock_id and lockee_id and includes the alias of their locker by locker_id
SELECT l.lock_id, l.lockee_id, l.locker_id, l.lock_priority, l.can_self_unlock, l.expires, l.password,
       pLocker.alias as locker_alias
FROM Locks l
JOIN Profiles pLocker ON l.locker_id = pLocker.id
WHERE l.lock_id = $1 AND l.lockee_id = $2;

-- name: IsLocked :one
-- Checks if a specific lock exists by lock_id and lockee_id
SELECT EXISTS(
    SELECT 1 FROM Locks
    WHERE lock_id = $1 AND lockee_id = $2
)::boolean as is_locked;

-- name: CanUnlockByLockId :one
-- Checks if a user can unlock a specific lock by lock ID, provided password, and user priority
SELECT CASE
    -- By definition, password trumps all. If a password is set, other settings are ignored
    WHEN l.password IS NOT NULL AND l.password = @password THEN TRUE
    -- Then if the 
    WHEN @unlocker = l.lockee_id THEN l.can_self_unlock
    WHEN @userPriority >= l.lock_priority THEN TRUE
    ELSE FALSE
END AS can_unlock
FROM Locks l
WHERE l.lock_id = @lockid AND l.lockee_id = @lockee;

-- name: AddOrUpdateLock :one
-- Adds a new lock for a user
INSERT INTO Locks (lock_id, lockee_id, locker_id, lock_priority, can_self_unlock, expires, password)
VALUES ($1, $2, $3, $4, $5, $6, $7)
ON CONFLICT (lock_id, lockee_id) DO UPDATE SET
    lock_priority = EXCLUDED.lock_priority,
    can_self_unlock = EXCLUDED.can_self_unlock,
    expires = EXCLUDED.expires,
    password = EXCLUDED.password
RETURNING lock_id, lockee_id, locker_id, lock_priority, can_self_unlock, expires, password;


-- name: RemoveLock :one
-- Removes a specific lock
DELETE FROM Locks
WHERE lock_id = $1 AND lockee_id = $2
RETURNING lock_id, lockee_id;

-- name: RemoveAllLocksForUser :many
-- Removes all locks for a specific user (used when user is deleted)
DELETE FROM Locks
WHERE lockee_id = $1 OR locker_id = $1
RETURNING lock_id, lockee_id;

-- name: PurgeExpiredLocks :many
-- Removes all locks that have expired
DELETE FROM Locks
WHERE expires IS NOT NULL AND expires < CURRENT_TIMESTAMP
RETURNING lock_id, lockee_id;

-- name: GetLocksForLocker :many
-- Retrieves all locks applied by a specific user (locker) and gets the alias of the lockee
SELECT l.lock_id, l.lockee_id, l.locker_id, l.lock_priority, l.can_self_unlock, l.expires, l.password,
       pLockee.alias as lockee_alias
FROM Locks l
JOIN Profiles pLockee ON l.lockee_id = pLockee.id
WHERE l.locker_id = $1;

-- name: GetLocksForPair :many
-- Retrieves all locks between two profile IDs (either where lockee=profile1 AND locker=profile2, OR lockee=profile2 AND locker=profile1)
SELECT l.lock_id, l.lockee_id, l.locker_id, l.lock_priority, l.can_self_unlock, l.expires, l.password
FROM Locks l
WHERE (l.lockee_id = $1 AND l.locker_id = $2)
   OR (l.lockee_id = $2 AND l.locker_id = $1);

-- name: HasExpiredLocks :one
-- Checks if there are any expired locks
SELECT EXISTS(
    SELECT 1 FROM Locks
    WHERE expires IS NOT NULL AND expires < CURRENT_TIMESTAMP
)::boolean as has_expired;

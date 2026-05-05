-- Debug/monitoring views for KinkLink database
-- Manually apply these for now as they disagree with sqlc

-- Drop existing views if they exist (idempotent)
DROP VIEW IF EXISTS vw_recent_accounts;
DROP VIEW IF EXISTS vw_user_profiles;
DROP VIEW IF EXISTS vw_pair_details;
DROP VIEW IF EXISTS vw_active_locks;
DROP VIEW IF EXISTS vw_user_wardrobe_aggregate;
DROP VIEW IF EXISTS vw_user_wardrobe_state;
DROP VIEW IF EXISTS vw_user_activewardrobe_status;
DROP VIEW IF EXISTS vw_data_integrity_issues;

-- Recent accounts (last 30 days)
CREATE VIEW vw_recent_accounts AS
SELECT id, discord_id, verified, banned, created_at, updated_at
FROM Users
WHERE created_at >= CURRENT_TIMESTAMP - INTERVAL '30 days'
ORDER BY created_at DESC;

-- User + profiles + ProfileConfig
CREATE VIEW vw_user_profiles AS
SELECT u.id AS user_id, u.discord_id, u.verified, u.banned,
       p.id AS profile_id, p.UID AS profile_uid, p.alias, p.title,
       pc.enable_glamours, pc.enable_garbler,
       pc.enable_garbler_channels, pc.enable_moodles
FROM Users u
LEFT JOIN Profiles p ON p.user_id = u.id
LEFT JOIN ProfileConfig pc ON pc.id = p.id
ORDER BY u.id, p.id;

-- Pair details with profile UIDs
CREATE VIEW vw_pair_details AS
SELECT pr.UID AS profile_uid, pp.UID AS paired_with_uid,
       p.expires, p.priority, p.controls_perm, p.controls_config,
       p.disable_safeword, p.interactions
FROM Pairs p
JOIN Profiles pr ON pr.id = p.id
JOIN Profiles pp ON pp.id = p.pair_id
ORDER BY pr.UID, pp.UID;

-- Active locks with user discord IDs
CREATE VIEW vw_active_locks AS
SELECT l.lock_id, l.expires, l.lock_priority, l.can_self_unlock,
       u1.discord_id AS lockee_discord, u2.discord_id AS locker_discord
FROM Locks l
JOIN Users u1 ON u1.id = l.lockee_id
JOIN Users u2 ON u2.id = l.locker_id
ORDER BY l.expires NULLS LAST;

-- Per-user wardrobe aggregate
CREATE VIEW vw_user_wardrobe_aggregate AS
SELECT p.uid AS profile_id, u.id AS user_id, u.discord_id,
       COUNT(w.id) AS total_items,
       COUNT(w.id) FILTER (WHERE w.type = 'item') AS item_count,
       COUNT(w.id) FILTER (WHERE w.type = 'set') AS set_count,
       COUNT(w.id) FILTER (WHERE w.type = 'moditem') AS moditem_count,
       MAX(w.updated_at) AS last_update
FROM Users u
LEFT JOIN Profiles p ON p.user_id = u.id
LEFT JOIN wardrobe w ON w.profile_id = p.id
GROUP BY p.uid, u.id, u.discord_id
ORDER BY p.uid;

-- Per-user wardrobe state (full item details, not aggregated metadata)
CREATE VIEW vw_user_wardrobe_state AS
SELECT 
    u.id AS user_id,
    u.discord_id,
    p.uid AS profile_uid,
    p.alias,
    w.*
FROM Users u
LEFT JOIN Profiles p ON p.user_id = u.id
LEFT JOIN wardrobe w ON w.profile_id = p.id
ORDER BY u.id, p.uid, w.id;

-- Per-user activewardrobe slot usage (shows if each slot is used or not)
CREATE VIEW vw_user_activewardrobe_status AS
SELECT 
    u.id AS user_id,
    p.uid AS profile_uid,
    p.alias,
    CASE WHEN aw.glamourerset IS NOT NULL THEN 'X' ELSE '' END AS set,
    CASE WHEN aw.head IS NOT NULL THEN 'X' ELSE '' END AS head,
    CASE WHEN aw.body IS NOT NULL THEN 'X' ELSE '' END AS body,
    CASE WHEN aw.hand IS NOT NULL THEN 'X' ELSE '' END AS hand,
    CASE WHEN aw.legs IS NOT NULL THEN 'X' ELSE '' END AS legs,
    CASE WHEN aw.feet IS NOT NULL THEN 'X' ELSE '' END AS feet,
    CASE WHEN aw.earring IS NOT NULL THEN 'X' ELSE '' END AS ear,
    CASE WHEN aw.neck IS NOT NULL THEN 'X' ELSE '' END AS neck,
    CASE WHEN aw.bracelet IS NOT NULL THEN 'X' ELSE '' END AS wrist,
    CASE WHEN aw.lring IS NOT NULL THEN 'X' ELSE '' END AS lring,
    CASE WHEN aw.rring IS NOT NULL THEN 'X' ELSE '' END AS rring,
    CASE WHEN aw.moditems IS NOT NULL THEN 'X' ELSE '' END AS mods,
    aw.id AS activewardrobe_id
FROM Users u
LEFT JOIN Profiles p ON p.user_id = u.id
LEFT JOIN activewardrobe aw ON aw.profile_id = p.id
ORDER BY u.id, p.uid;

-- Data integrity checks
CREATE VIEW vw_data_integrity_issues AS
-- Users with no profiles
SELECT 'user_no_profiles' AS issue_type, u.id::text AS record_id,
       'User ' || u.discord_id || ' has no profiles' AS description
FROM Users u
LEFT JOIN Profiles p ON p.user_id = u.id
WHERE p.id IS NULL
UNION ALL
-- Profiles with no ProfileConfig
SELECT 'profile_no_config' AS issue_type, p.id::text AS record_id,
       'Profile ' || p.UID || ' has no config entry' AS description
FROM Profiles p
LEFT JOIN ProfileConfig pc ON pc.id = p.id
WHERE pc.id IS NULL
UNION ALL
-- Expired pairs still in table
SELECT 'expired_pair' AS issue_type, p.id::text || '_' || p.pair_id::text AS record_id,
       'Pair ' || pr.UID || ' -> ' || pp.UID || ' expired at ' || p.expires AS description
FROM Pairs p
JOIN Profiles pr ON pr.id = p.id
JOIN Profiles pp ON pp.id = p.pair_id
WHERE p.expires IS NOT NULL AND p.expires < CURRENT_TIMESTAMP
UNION ALL
-- Locks on banned users
SELECT 'lock_on_banned_user' AS issue_type, l.lock_id AS record_id,
       'Lock ' || l.lock_id || ' on banned user ' || u.discord_id AS description
FROM Locks l
JOIN Users u ON u.id = l.lockee_id
WHERE u.banned = true
ORDER BY issue_type, record_id;

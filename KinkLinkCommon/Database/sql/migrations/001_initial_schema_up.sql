-- use PSQL extension for encrypting passwords (lower risk)
CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- TODO: Add cryptography module to postgres migrations for security
-- Admin accounts are stored in a separate table for security purposes.
-- There is no overlap in this and the other accounts with separate
-- Needs admin table to check for Admins
CREATE TABLE IF NOT EXISTS Admin (
    id SERIAL PRIMARY KEY,
    name VARCHAR(256) NOT NULL,
    password VARCHAR(256) NOT NULL,
    discord_id BIGINT NOT NULL
);

-- This contains the user accounts for kinklink
-- Verified/banned are used to track and purge accounts based on settings
CREATE TABLE IF NOT EXISTS Users (
    id SERIAL PRIMARY KEY,
    discord_id BIGINT NOT NULL UNIQUE,
    secret_key_hash VARCHAR(64),
    verified bool DEFAULT false,
    banned bool DEFAULT false,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- This is used for separate character profiles.
-- Each user may have many profiles with a unique identifier
-- Each profile contains: name, description, picture, and foreign key to user.id
CREATE TABLE IF NOT EXISTS Profiles (
    id SERIAL PRIMARY KEY,
    user_id INTEGER NOT NULL REFERENCES Users(id) ON DELETE CASCADE,
    UID VARCHAR(10) NOT NULL,
    chat_role VARCHAR(20) DEFAULT '',
    alias VARCHAR(20) DEFAULT '',
    title VARCHAR(20) DEFAULT 'Kinkster',
    description TEXT
);

-- This is the pairs table used to link UUIDs
-- The pair permissions can expire to facilitate temporary pairings
-- Each individual permission is just a boolean value for simplicity
CREATE TABLE IF NOT EXISTS Pairs (
    id SERIAL NOT NULL REFERENCES Profiles(id) ON DELETE CASCADE,
    pair_id INTEGER NOT NULL REFERENCES Profiles(id) ON DELETE CASCADE,
    PRIMARY KEY (id, pair_id),
    expires TIMESTAMP,

    toggle_timer_locks BOOL DEFAULT false NOT NULL,
    toggle_permanent_locks BOOL DEFAULT false NOT NULL,

    toggle_garbler BOOL DEFAULT false NOT NULL,
    lock_garbler BOOL DEFAULT false NOT NULL,
    toggle_channels BOOL DEFAULT false NOT NULL,
    lock_channels BOOL DEFAULT false NOT NULL,
    
    apply_gag BOOL DEFAULT false NOT NULL,
    lock_gag BOOL DEFAULT false NOT NULL,
    unlock_gag BOOL DEFAULT false NOT NULL,
    remove_gag BOOL DEFAULT false NOT NULL,

    apply_wardrobe BOOL DEFAULT false NOT NULL,
    lock_wardrobe BOOL DEFAULT false NOT NULL,
    unlock_wardrobe BOOL DEFAULT false NOT NULL,
    remove_wardrobe BOOL DEFAULT false NOT NULL,

    apply_moodles BOOL DEFAULT false NOT NULL,
    lock_moodles BOOL DEFAULT false NOT NULL,
    unlock_moodles BOOL DEFAULT false NOT NULL,
    remove_moodles BOOL DEFAULT false NOT NULL
);

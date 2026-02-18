DROP TABLE IF EXISTS Pairs;
-- This is the pairs table used to link UUIDs
-- The pair permissions can expire to facilitate temporary pairings
-- Eac  individual permission is just a BOOLEANean value for simplicity
CREATE TABLE IF NOT EXISTS Pairs (
    id INTEGER NOT NULL REFERENCES Profiles(id) ON DELETE CASCADE,
    pair_id INTEGER NOT NULL REFERENCES Profiles(id) ON DELETE CASCADE,
    PRIMARY KEY (id, pair_id),
    expires TIMESTAMP,
    priority INTEGER DEFAULT 0,
    controls_perm BOOLEAN DEFAULT FALSE,
    controls_config BOOLEAN DEFAULT FALSE,
    disable_safeword BOOLEAN DEFAULT FALSE,
    -- Bitmask for all the interactions permissions
    interactions BIGINT DEFAULT 0
);

CREATE TABLE IF NOT EXISTS ProfileConfig (
    id INTEGER UNIQUE NOT NULL REFERENCES Profiles(id) ON DELETE CASCADE,
    enable_glamours BOOLEAN DEFAULT false,
    enable_garbler BOOLEAN DEFAULT false,
    enable_garbler_channels BOOLEAN DEFAULT false,
    enable_moodles BOOLEAN DEFAULT false
);

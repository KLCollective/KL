DROP TABLE IF EXISTS Pairs;
-- This is the pairs table used to link UUIDs
-- The pair permissions can expire to facilitate temporary pairings
-- Each individual permission is just a boolean value for simplicity
CREATE TABLE IF NOT EXISTS Pairs (
    id SERIAL NOT NULL REFERENCES Profiles(id) ON DELETE CASCADE,
    pair_id INTEGER NOT NULL REFERENCES Profiles(id) ON DELETE CASCADE,
    PRIMARY KEY (id, pair_id),
    expires TIMESTAMP,
    priority INTEGER NOT NULL,
    gags INTEGER NOT NULL,
    wardrobe INTEGER NOT NULL,
    moodles INTEGER NOT NULL
);


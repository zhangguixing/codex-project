-- Apply once to databases created before the P0 activity lifecycle update.
ALTER TABLE activity_join
    ADD COLUMN checked_in_at DATETIME NULL AFTER canceled_at;
ALTER TABLE activity
    ADD COLUMN duration_minutes INT NULL AFTER newbie_friendly;
ALTER TABLE activity
    ADD COLUMN difficulty VARCHAR(16) NULL AFTER duration_minutes;
ALTER TABLE activity
    ADD COLUMN language VARCHAR(32) NULL AFTER difficulty;
ALTER TABLE activity
    ADD COLUMN teaching_provided TINYINT NOT NULL DEFAULT 0 AFTER language;
ALTER TABLE activity
    ADD COLUMN bring_game TINYINT NOT NULL DEFAULT 0 AFTER teaching_provided;
ALTER TABLE game_store
    ADD COLUMN facilities VARCHAR(500) NULL AFTER description;
ALTER TABLE game_store
    ADD COLUMN parking_available TINYINT NOT NULL DEFAULT 0 AFTER facilities;

CREATE TABLE IF NOT EXISTS activity_chat
(
    id          BIGINT PRIMARY KEY AUTO_INCREMENT,
    activity_id BIGINT       NOT NULL,
    user_id     BIGINT       NOT NULL,
    content     VARCHAR(500) NOT NULL,
    created_at  DATETIME     NOT NULL,
    INDEX       idx_chat_activity_time(activity_id, created_at)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS activity_reminder_log
(
    id
    BIGINT
    PRIMARY
    KEY
    AUTO_INCREMENT,
    activity_id
    BIGINT
    NOT
    NULL,
    user_id
    BIGINT
    NOT
    NULL,
    reminder_type
    VARCHAR
(
    16
) NOT NULL,
    created_at DATETIME NOT NULL,
    UNIQUE KEY uk_reminder_activity_user_type
(
    activity_id,
    user_id,
    reminder_type
)
    ) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS user_block
(
    id
    BIGINT
    PRIMARY
    KEY
    AUTO_INCREMENT,
    user_id
    BIGINT
    NOT
    NULL,
    blocked_user_id
    BIGINT
    NOT
    NULL,
    created_at
    DATETIME
    NOT
    NULL,
    UNIQUE
    KEY
    uk_user_block
(
    user_id,
    blocked_user_id
),
    INDEX idx_blocked_user
(
    blocked_user_id
)
    ) ENGINE=InnoDB;

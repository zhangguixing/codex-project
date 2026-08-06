CREATE
DATABASE IF NOT EXISTS zhuoyouquan DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE
zhuoyouquan;

CREATE TABLE t_user
(
    id            BIGINT PRIMARY KEY AUTO_INCREMENT,
    openid        VARCHAR(64) NOT NULL UNIQUE,
    nickname      VARCHAR(32) NOT NULL,
    avatar        VARCHAR(512),
    phone         VARCHAR(32),
    city          VARCHAR(32),
    district      VARCHAR(32),
    bio           VARCHAR(200),
    game_level    VARCHAR(20) NOT NULL DEFAULT '新手玩家',
    created_at    DATETIME    NOT NULL,
    last_login_at DATETIME,
    deleted       TINYINT     NOT NULL DEFAULT 0,
    INDEX         idx_user_city(city)
) ENGINE=InnoDB;
CREATE TABLE game_type
(
    id         BIGINT PRIMARY KEY AUTO_INCREMENT,
    name       VARCHAR(32) NOT NULL UNIQUE,
    sort_order INT         NOT NULL DEFAULT 0,
    enabled    TINYINT     NOT NULL DEFAULT 1
) ENGINE=InnoDB;
CREATE TABLE activity
(
    id                BIGINT PRIMARY KEY AUTO_INCREMENT,
    organizer_id      BIGINT         NOT NULL,
    game_type_id      BIGINT         NOT NULL,
    title             VARCHAR(80)    NOT NULL,
    cover_url         VARCHAR(512),
    start_time        DATETIME       NOT NULL,
    max_people        INT            NOT NULL,
    joined_people     INT            NOT NULL DEFAULT 0,
    city              VARCHAR(32)    NOT NULL,
    store_name        VARCHAR(80),
    address           VARCHAR(255)   NOT NULL,
    longitude         DECIMAL(10, 7),
    latitude          DECIMAL(10, 7),
    fee               DECIMAL(10, 2) NOT NULL DEFAULT 0,
    aa                TINYINT        NOT NULL DEFAULT 1,
    description       VARCHAR(2000)  NOT NULL,
    newbie_friendly   TINYINT        NOT NULL DEFAULT 0,
    duration_minutes  INT,
    difficulty        VARCHAR(16),
    language          VARCHAR(32),
    teaching_provided TINYINT        NOT NULL DEFAULT 0,
    bring_game        TINYINT        NOT NULL DEFAULT 0,
    status            VARCHAR(16)    NOT NULL DEFAULT 'OPEN',
    created_at        DATETIME       NOT NULL,
    updated_at        DATETIME       NOT NULL,
    deleted           TINYINT        NOT NULL DEFAULT 0,
    INDEX             idx_activity_city_time(city,start_time),
    INDEX             idx_activity_game(game_type_id),
    CONSTRAINT fk_activity_organizer FOREIGN KEY (organizer_id) REFERENCES t_user (id),
    CONSTRAINT fk_activity_game FOREIGN KEY (game_type_id) REFERENCES game_type (id)
) ENGINE=InnoDB;
CREATE TABLE activity_join
(
    id            BIGINT PRIMARY KEY AUTO_INCREMENT,
    activity_id   BIGINT      NOT NULL,
    user_id       BIGINT      NOT NULL,
    status        VARCHAR(16) NOT NULL,
    created_at    DATETIME    NOT NULL,
    canceled_at   DATETIME,
    checked_in_at DATETIME,
    UNIQUE KEY uk_activity_user(activity_id,user_id),
    INDEX         idx_join_user(user_id,status),
    CONSTRAINT fk_join_activity FOREIGN KEY (activity_id) REFERENCES activity (id),
    CONSTRAINT fk_join_user FOREIGN KEY (user_id) REFERENCES t_user (id)
) ENGINE=InnoDB;
CREATE TABLE user_credit
(
    id         BIGINT PRIMARY KEY AUTO_INCREMENT,
    user_id    BIGINT      NOT NULL UNIQUE,
    score      INT         NOT NULL DEFAULT 100,
    level      VARCHAR(20) NOT NULL DEFAULT '优秀玩家',
    updated_at DATETIME    NOT NULL,
    CONSTRAINT fk_credit_user FOREIGN KEY (user_id) REFERENCES t_user (id)
) ENGINE=InnoDB;
CREATE TABLE user_game
(
    id           BIGINT PRIMARY KEY AUTO_INCREMENT,
    user_id      BIGINT NOT NULL,
    game_type_id BIGINT NOT NULL,
    UNIQUE KEY uk_user_game(user_id,game_type_id),
    CONSTRAINT fk_user_game_user FOREIGN KEY (user_id) REFERENCES t_user (id),
    CONSTRAINT fk_user_game_type FOREIGN KEY (game_type_id) REFERENCES game_type (id)
) ENGINE=InnoDB;
CREATE TABLE favorite
(
    id          BIGINT PRIMARY KEY AUTO_INCREMENT,
    user_id     BIGINT      NOT NULL,
    target_type VARCHAR(16) NOT NULL,
    target_id   BIGINT      NOT NULL,
    created_at  DATETIME    NOT NULL,
    UNIQUE KEY uk_favorite(user_id,target_type,target_id),
    INDEX       idx_favorite_target(target_type,target_id),
    CONSTRAINT fk_favorite_user FOREIGN KEY (user_id) REFERENCES t_user (id)
) ENGINE=InnoDB;
CREATE TABLE user_comment
(
    id                  BIGINT PRIMARY KEY AUTO_INCREMENT,
    activity_id         BIGINT   NOT NULL,
    from_user_id        BIGINT   NOT NULL,
    to_user_id          BIGINT   NOT NULL,
    punctual_score      TINYINT  NOT NULL,
    friendly_score      TINYINT  NOT NULL,
    skill_score         TINYINT  NOT NULL,
    communication_score TINYINT  NOT NULL,
    content             VARCHAR(500),
    created_at          DATETIME NOT NULL,
    UNIQUE KEY uk_comment(activity_id,from_user_id,to_user_id)
) ENGINE=InnoDB;
CREATE TABLE message
(
    id         BIGINT PRIMARY KEY AUTO_INCREMENT,
    user_id    BIGINT       NOT NULL,
    type       VARCHAR(32)  NOT NULL,
    title      VARCHAR(100) NOT NULL,
    content    VARCHAR(500),
    target_id  BIGINT,
    read_at    DATETIME,
    created_at DATETIME     NOT NULL,
    INDEX      idx_message_user(user_id,read_at)
) ENGINE=InnoDB;
CREATE TABLE activity_chat
(
    id               BIGINT PRIMARY KEY AUTO_INCREMENT,
    activity_id      BIGINT       NOT NULL,
    user_id          BIGINT       NOT NULL,
    parent_id        BIGINT,
    reply_to_user_id BIGINT,
    content          VARCHAR(500) NOT NULL,
    created_at       DATETIME     NOT NULL,
    INDEX       idx_chat_activity_time(activity_id, created_at)
) ENGINE=InnoDB;
CREATE TABLE activity_reminder_log
(
    id            BIGINT PRIMARY KEY AUTO_INCREMENT,
    activity_id   BIGINT      NOT NULL,
    user_id       BIGINT      NOT NULL,
    reminder_type VARCHAR(16) NOT NULL,
    created_at    DATETIME    NOT NULL,
    UNIQUE KEY uk_reminder_activity_user_type(activity_id, user_id, reminder_type)
) ENGINE=InnoDB;
CREATE TABLE game_store
(
    id                BIGINT PRIMARY KEY AUTO_INCREMENT,
    name              VARCHAR(100) NOT NULL,
    city              VARCHAR(32)  NOT NULL,
    address           VARCHAR(255) NOT NULL,
    longitude         DECIMAL(10, 7),
    latitude          DECIMAL(10, 7),
    business_hours    VARCHAR(100),
    description       VARCHAR(1000),
    facilities        VARCHAR(500),
    parking_available TINYINT      NOT NULL DEFAULT 0,
    status            VARCHAR(16)  NOT NULL DEFAULT 'PENDING',
    created_at        DATETIME     NOT NULL
) ENGINE=InnoDB;
CREATE TABLE report
(
    id          BIGINT PRIMARY KEY AUTO_INCREMENT,
    reporter_id BIGINT       NOT NULL,
    target_type VARCHAR(16)  NOT NULL,
    target_id   BIGINT       NOT NULL,
    reason      VARCHAR(500) NOT NULL,
    status      VARCHAR(16)  NOT NULL DEFAULT 'PENDING',
    created_at  DATETIME     NOT NULL,
    handled_at  DATETIME,
    INDEX       idx_report_status(status)
) ENGINE=InnoDB;
CREATE TABLE user_block
(
    id              BIGINT PRIMARY KEY AUTO_INCREMENT,
    user_id         BIGINT   NOT NULL,
    blocked_user_id BIGINT   NOT NULL,
    created_at      DATETIME NOT NULL,
    UNIQUE KEY uk_user_block(user_id, blocked_user_id),
    INDEX           idx_blocked_user(blocked_user_id)
) ENGINE=InnoDB;
INSERT INTO game_type(name, sort_order)
VALUES ('狼人杀', 1),
       ('剧本杀', 2),
       ('卡牌', 3),
       ('三国杀', 4),
       ('卡坦岛', 5),
       ('UNO', 6),
       ('麻将', 7),
       ('其他', 99);

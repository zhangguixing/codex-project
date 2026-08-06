package com.zhuoyouquan.vo;

import lombok.Data;

import java.math.BigDecimal;
import java.time.LocalDateTime;
import java.util.List;

@Data
public class ActivityVO {
    private Long id;
    private String title;
    private String coverUrl;
    private String gameType;
    private LocalDateTime startTime;
    private Integer maxPeople;
    private Integer joinedPeople;
    private String city;
    private String storeName;
    private String address;
    private BigDecimal longitude;
    private BigDecimal latitude;
    private BigDecimal fee;
    private Boolean aa;
    private String description;
    private Boolean newbieFriendly;
    private Integer durationMinutes;
    private String difficulty;
    private String language;
    private Boolean teachingProvided;
    private Boolean bringGame;
    private String status;
    private LocalDateTime createdAt;
    private UserVO organizer;
    private List<UserVO> participants;
    private List<Long> checkedInParticipantIds;
    private Boolean joined;
    private Boolean waitlisted;
    private Integer waitlistCount;
    private Boolean favorited;
}

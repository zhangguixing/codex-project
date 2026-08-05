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
    private String status;
    private LocalDateTime createdAt;
    private UserVO organizer;
    private List<UserVO> participants;
    private Boolean joined;
    private Boolean favorited;
}

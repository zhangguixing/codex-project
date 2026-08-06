package com.zhuoyouquan.dto;

import lombok.Data;

import java.math.BigDecimal;

@Data
public class ActivityQuery {
    private String city;
    private String keyword;
    private Long gameTypeId;
    private String timeRange;
    private String sort = "latest";
    private Double latitude;
    private Double longitude;
    private Boolean newbieFriendly;
    private String difficulty;
    private BigDecimal maxFee;
    private Integer minSlots;
    private Integer page = 1;
    private Integer size = 10;
}

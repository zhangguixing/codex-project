package com.zhuoyouquan.dto;

import lombok.Data;

@Data
public class ActivityQuery {
    private String city;
    private Long gameTypeId;
    private String timeRange;
    private String sort = "latest";
    private Double latitude;
    private Double longitude;
    private Integer page = 1;
    private Integer size = 10;
}

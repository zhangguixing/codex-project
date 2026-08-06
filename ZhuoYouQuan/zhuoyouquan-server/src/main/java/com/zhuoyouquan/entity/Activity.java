package com.zhuoyouquan.entity;

import com.baomidou.mybatisplus.annotation.IdType;
import com.baomidou.mybatisplus.annotation.TableId;
import com.baomidou.mybatisplus.annotation.TableLogic;
import com.baomidou.mybatisplus.annotation.TableName;
import lombok.Data;

import java.math.BigDecimal;
import java.time.LocalDateTime;

@Data
@TableName("activity")
public class Activity {
    @TableId(type = IdType.AUTO)
    private Long id;
    private Long organizerId;
    private Long gameTypeId;
    private String title;
    private String coverUrl;
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
    private LocalDateTime updatedAt;
    @TableLogic
    private Integer deleted;
}

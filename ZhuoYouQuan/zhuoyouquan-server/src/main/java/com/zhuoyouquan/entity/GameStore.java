package com.zhuoyouquan.entity;

import com.baomidou.mybatisplus.annotation.IdType;
import com.baomidou.mybatisplus.annotation.TableId;
import com.baomidou.mybatisplus.annotation.TableName;
import lombok.Data;

import java.math.BigDecimal;
import java.time.LocalDateTime;

@Data
@TableName("game_store")
public class GameStore {
    @TableId(type = IdType.AUTO)
    private Long id;
    private String name;
    private String city;
    private String address;
    private BigDecimal longitude;
    private BigDecimal latitude;
    private String businessHours;
    private String description;
    private String facilities;
    private Boolean parkingAvailable;
    private String status;
    private LocalDateTime createdAt;
}

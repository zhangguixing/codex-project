package com.zhuoyouquan.entity;

import com.baomidou.mybatisplus.annotation.IdType;
import com.baomidou.mybatisplus.annotation.TableId;
import com.baomidou.mybatisplus.annotation.TableName;
import lombok.Data;

import java.time.LocalDateTime;

@Data
@TableName("user_comment")
public class UserComment {
    @TableId(type = IdType.AUTO)
    private Long id;
    private Long activityId;
    private Long fromUserId;
    private Long toUserId;
    private Integer punctualScore;
    private Integer friendlyScore;
    private Integer skillScore;
    private Integer communicationScore;
    private String content;
    private LocalDateTime createdAt;
}

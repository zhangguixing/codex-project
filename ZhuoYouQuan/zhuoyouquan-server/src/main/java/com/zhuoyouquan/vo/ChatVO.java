package com.zhuoyouquan.vo;

import lombok.Data;

import java.time.LocalDateTime;

@Data
public class ChatVO {
    private Long id;
    private Long parentId;
    private String content;
    private LocalDateTime createdAt;
    private UserVO user;
    private UserVO replyToUser;
}

package com.zhuoyouquan.entity;

import com.baomidou.mybatisplus.annotation.IdType;
import com.baomidou.mybatisplus.annotation.TableId;
import com.baomidou.mybatisplus.annotation.TableName;
import lombok.Data;

@Data
@TableName("user_game")
public class UserGame {
    @TableId(type = IdType.AUTO)
    private Long id;
    private Long userId;
    private Long gameTypeId;
}

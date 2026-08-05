package com.zhuoyouquan.vo;

import lombok.AllArgsConstructor;
import lombok.Data;

@Data
@AllArgsConstructor
public class AuthVO {
    private String token;
    private UserVO user;
}

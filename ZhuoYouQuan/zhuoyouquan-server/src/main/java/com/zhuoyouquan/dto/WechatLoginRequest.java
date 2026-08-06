package com.zhuoyouquan.dto;

import jakarta.validation.constraints.NotBlank;
import lombok.Data;

@Data
public class WechatLoginRequest {
    @NotBlank(message = "微信登录凭证不能为空")
    private String code;
}

package com.zhuoyouquan.dto;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Size;
import lombok.Data;

@Data
public class ActivityMessageRequest {
    @NotBlank(message = "留言内容不能为空")
    @Size(max = 500, message = "留言不能超过500个字符")
    private String content;
}

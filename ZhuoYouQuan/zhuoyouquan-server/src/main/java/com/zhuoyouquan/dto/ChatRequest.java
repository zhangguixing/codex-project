package com.zhuoyouquan.dto;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Size;
import lombok.Data;

@Data
public class ChatRequest {
    private Long parentId;

    @NotBlank
    @Size(max = 500)
    private String content;
}

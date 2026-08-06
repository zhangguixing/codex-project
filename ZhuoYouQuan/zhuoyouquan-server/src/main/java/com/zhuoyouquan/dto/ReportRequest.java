package com.zhuoyouquan.dto;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Size;
import lombok.Data;

@Data
public class ReportRequest {
    @NotBlank
    private String targetType;
    @NotNull
    private Long targetId;
    @NotBlank
    @Size(max = 500)
    private String reason;
}

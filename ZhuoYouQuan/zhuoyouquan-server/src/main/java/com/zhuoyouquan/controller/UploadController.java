package com.zhuoyouquan.controller;

import com.zhuoyouquan.common.BizException;
import com.zhuoyouquan.common.Result;
import jakarta.servlet.http.HttpServletRequest;
import org.springframework.http.MediaType;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;
import org.springframework.web.multipart.MultipartFile;

import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.StandardCopyOption;
import java.util.Map;
import java.util.Set;
import java.util.UUID;

@RestController
@RequestMapping("/api/uploads")
public class UploadController {
    private static final long MAX_COVER_BYTES = 5 * 1024 * 1024;
    private static final String IMAGE_WEBP = "image/webp";
    private static final Set<String> TYPES = Set.of(MediaType.IMAGE_JPEG_VALUE, MediaType.IMAGE_PNG_VALUE, IMAGE_WEBP);

    @PostMapping("/activity-cover")
    public Result<Map<String, String>> activityCover(HttpServletRequest request, @RequestParam("file") MultipartFile file) {
        if (file.isEmpty() || file.getSize() > MAX_COVER_BYTES || !TYPES.contains(file.getContentType())) throw new BizException("请上传不超过 5MB 的 JPG、PNG 或 WEBP 图片");
        try {
            String extension = MediaType.IMAGE_PNG_VALUE.equals(file.getContentType()) ? ".png" : IMAGE_WEBP.equals(file.getContentType()) ? ".webp" : ".jpg";
            String filename = UUID.randomUUID() + extension;
            Path directory = Path.of("uploads", "activity-covers");
            Files.createDirectories(directory);
            Files.copy(file.getInputStream(), directory.resolve(filename), StandardCopyOption.REPLACE_EXISTING);
            String relative = "/uploads/activity-covers/" + filename;
            String base = request.getRequestURL().toString().replace(request.getRequestURI(), "");
            return Result.ok(Map.of("url", base + relative));
        } catch (Exception e) { throw new BizException("封面上传失败"); }
    }
}

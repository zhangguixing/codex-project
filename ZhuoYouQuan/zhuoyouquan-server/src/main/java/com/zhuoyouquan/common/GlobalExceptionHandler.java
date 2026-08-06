package com.zhuoyouquan.common;

import jakarta.validation.ConstraintViolationException;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.MethodArgumentNotValidException;
import org.springframework.web.bind.annotation.ExceptionHandler;
import org.springframework.web.bind.annotation.RestControllerAdvice;

@RestControllerAdvice
public class GlobalExceptionHandler {
    @ExceptionHandler(BizException.class)
    public ResponseEntity<Result<Void>> business(BizException e) {
        return ResponseEntity.badRequest().body(Result.fail(400, e.getMessage()));
    }

    @ExceptionHandler({
            MethodArgumentNotValidException.class, ConstraintViolationException.class
    }
    )
    public ResponseEntity<Result<Void>> validation(Exception e) {
        String message = e instanceof MethodArgumentNotValidException m && m.getBindingResult().getFieldError() != null
                ? m.getBindingResult().getFieldError().getDefaultMessage() : e.getMessage();
        return ResponseEntity.badRequest().body(Result.fail(400, message));
    }

    @ExceptionHandler(Exception.class)
    public ResponseEntity<Result<Void>> unexpected(Exception e) {
        return ResponseEntity.internalServerError().body(Result.fail(500, "服务器繁忙，请稍后重试"));
    }
}

#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

char *ConvertUnixToWinePath(const char* unixPath) {

    HWND hWnd = GetConsoleWindow();
    if (hWnd) {
        ShowWindow(hWnd, SW_HIDE);
    }

    size_t len = strlen(unixPath);

    char* win = (char*)malloc(len + 3);
    if (!win) return NULL;

    win[0] = 'Z';
    win[1] = ':';
    win[2] = '\\';

    for (size_t i = 0; i < len; i++) {
        win[i + 3] = (unixPath[i] == '/') ? '\\' : unixPath[i];
    }

    win[len + 3] = '\0';
    return win;
}

int main(void) {
    const char *filename = "instancepath.txt";

    FILE* file = fopen(filename, "rb");
    if (!file) {
        fprintf(stderr, "ERROR: cannot open %s\n", filename);
        return -1;
    }

    fseek(file, 0, SEEK_END);
    long sz = ftell(file);
    if (sz <= 0 || sz > 4096) {
        fclose(file);
        fprintf(stderr, "ERROR: invalid file size\n");
        return -1;
    }
    fseek(file, 0, SEEK_SET);

    char* buffer = (char*)malloc(sz + 1);
    if (!buffer) {
        fclose(file);
        fprintf(stderr, "ERROR: malloc failed\n");
        return -1;
    }

    size_t bytes = fread(buffer, 1, sz, file);
    buffer[bytes] = '\0';
    fclose(file);

    for (size_t i = 0; i < bytes; i++) {
        if (buffer[i] == '\r' || buffer[i] == '\n') {
            buffer[i] = '\0';
            break;
        }
    }

    if (buffer[0] == '\0') {
        fprintf(stderr, "ERROR: empty path in file\n");
        free(buffer);
        return -1;
    }

    char *winePath = ConvertUnixToWinePath(buffer);
    free(buffer);

    if (!winePath) {
        fprintf(stderr, "ERROR: path conversion failed\n");
        return -1;
    }

    size_t len = strlen(winePath);
    char *cmd = (char*)malloc(len + 3);
    if (!cmd) {
        free(winePath);
        fprintf(stderr, "ERROR: malloc failed for cmd\n");
        return -1;
    }

    cmd[0] = '"';
    memcpy(cmd + 1, winePath, len);
    cmd[len + 1] = '"';
    cmd[len + 2] = '\0';

    free(winePath);

    STARTUPINFOA si;
    PROCESS_INFORMATION pi;
    ZeroMemory(&si, sizeof(si));
    ZeroMemory(&pi, sizeof(pi));
    si.cb = sizeof(si);

    BOOL ok = CreateProcessA(
        NULL,
        cmd,
        NULL, NULL,
        FALSE,
        0,
        NULL,
        NULL,
        &si,
        &pi
    );

    free(cmd);

    if (!ok) {
        fprintf(stderr, "ERROR: CreateProcess failed (%lu)\n", GetLastError());
        return -1;
    }

    WaitForSingleObject(pi.hProcess, INFINITE);

    DWORD exitCode = 0;
    GetExitCodeProcess(pi.hProcess, &exitCode);

    CloseHandle(pi.hThread);
    CloseHandle(pi.hProcess);

    return (int)exitCode;
}

# Указываем пути (взято из ваших данных сборки)
$NDK = "C:\Program Files\Unity\Hub\Editor\2022.3.8f1\Editor\Data\PlaybackEngines\AndroidPlayer\NDK"
$OPENCV_SDK = "C:\Users\Constantin Nauman\opencv"
$OUTPUT_DIR = "C:\Users\Constantin Nauman\mr-example-meta-openxr-main\Assets\Plugins\Android\libs\arm64-v8a"

# Поиск компилятора
$CC_LIST = Get-ChildItem -Path "$NDK\toolchains\llvm\prebuilt\windows-x86_64\bin\aarch64-linux-android*-clang++.exe"
if ($null -eq $CC_LIST) {
    Write-Error "Компилятор не найден! Проверьте папку NDK."
    return
}
$CC = $CC_LIST[0].FullName

# Параметры сборки (через массив, чтобы избежать проблем с кавычками в PS)
$compileArgs = @(
    "-shared", "-fPIC",
    "EyeBridge.cpp",
    "-I$OPENCV_SDK\include",
    "-I$OPENCV_SDK\modules\core\include",
    "-I$OPENCV_SDK\modules\imgproc\include",
    "-L$OUTPUT_DIR",
    "-lopencv_world",
    "-Wl,-rpath,`$ORIGIN",
    "-o", "$OUTPUT_DIR\libEyeBridge.so"
)

Write-Host "Запуск сборки для Quest 3..." -ForegroundColor Cyan
& $CC @compileArgs
if ($?) { Write-Host "УСПЕХ! Файл создан в $OUTPUT_DIR" -ForegroundColor Green }
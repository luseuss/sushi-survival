#!/usr/bin/env python3
"""폴더를 윈도우 탐색기의 기본 압축 풀기로도 열리는 진짜 zip으로 묶는다.

  사용법: python tools/make_zip.py <묶을 폴더> <만들 zip 경로>

왜 따로 만드나: Git Bash의 `tar -a -c -f x.zip`은 GNU tar라서 zip 형식을 못 만들고 확장자만 .zip인 tar 파일을 만든다
(v1.0~v1.6이 그랬다). 탐색기·Expand-Archive는 그걸 못 열고 7-Zip·Bandizip만 열 수 있다.
여기서는 표준 deflate zip을 만든다 — 항목 이름은 폴더 기준 상대 경로(`./` 없이, `/` 구분), 한글 이름은 UTF-8로 기록한다.
"""
import os
import sys
import zipfile


def main() -> int:
    # 한글 메시지가 윈도우 콘솔 기본 인코딩에서 깨지지 않게 한다.
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")
        sys.stderr.reconfigure(encoding="utf-8")

    if len(sys.argv) != 3:
        print(__doc__)
        return 2

    source = os.path.abspath(sys.argv[1])
    target = os.path.abspath(sys.argv[2])

    if not os.path.isdir(source):
        print(f"묶을 폴더가 없습니다: {source}", file=sys.stderr)
        return 1

    if os.path.exists(target):
        os.remove(target)

    count = 0
    with zipfile.ZipFile(target, "w", compression=zipfile.ZIP_DEFLATED, compresslevel=6) as archive:
        for folder, dirs, files in os.walk(source):
            dirs.sort()
            for name in sorted(files):
                path = os.path.join(folder, name)
                arcname = os.path.relpath(path, source).replace(os.sep, "/")
                archive.write(path, arcname)
                count += 1

    # 만든 뒤 바로 다시 열어 깨진 곳이 없는지 확인한다.
    with zipfile.ZipFile(target) as archive:
        bad = archive.testzip()
        if bad is not None:
            print(f"zip 검사 실패: {bad}", file=sys.stderr)
            return 1

    size_mb = os.path.getsize(target) / (1024 * 1024)
    print(f"만들었습니다: {target} ({count}개 파일, {size_mb:.0f}MB)")
    return 0


if __name__ == "__main__":
    sys.exit(main())

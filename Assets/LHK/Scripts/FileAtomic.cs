using System;
using System.IO;

public static class FileAtomic
{
    // 원자적 쓰기: 임시 파일에 먼저 기록 후, 완료되면 대상 파일로 교체
    public static void WriteAllBytesAtomic(string path, byte[] data)
    {
        // 임시 파일 경로 생성 (확장자 .tmp)
        string tempPath = path + ".tmp";

        // 1) 임시 파일에 전체 데이터를 기록
        File.WriteAllBytes(tempPath, data);

        // 2) 기존 파일이 있으면 삭제 (플랫폼 별 예외 대비)
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        // 3) 임시 파일을 최종 파일명으로 이동(사실상 rename/replace)
        File.Move(tempPath, path);
    }
}

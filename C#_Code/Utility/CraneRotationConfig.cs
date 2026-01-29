using UnityEngine;
using System.IO;
using System.Text;
using System;

public class CraneRotationConfig : MonoBehaviour
{
    [Header("설정")]
    [Tooltip("CsConfig.ini 파일에 있는 크레인 이름 (복사붙여넣기 하세요)")]
    public string craneNameInConfig;

    [Tooltip("파일 확인 주기 (초)")]
    public float reloadInterval = 1.0f;

    [Header("상태 확인 (Read Only)")]
    [SerializeField] // 인스펙터에서 현재 적용된 값만 확인 가능
    private float currentAppliedAngle = 0f;

    private float timer = 0f;
    private string filePath;

    void Start()
    {
        // _Data 폴더 경로 설정
        filePath = Path.Combine(Application.dataPath, "CsConfig.ini");
        UpdateRotationFromFile();
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= reloadInterval)
        {
            timer = 0f;
            UpdateRotationFromFile();
        }

        // 현재 각도를 계속 유지
        Vector3 currentRot = transform.localEulerAngles;
        transform.localEulerAngles = new Vector3(currentRot.x, currentRot.y, currentAppliedAngle);
    }

    // OnGUI(노란 글씨 출력 부분)는 삭제되었습니다.

    private void UpdateRotationFromFile()
    {
        if (!File.Exists(filePath))
        {
            return;
        }

        try
        {
            // [수정됨] ANSI 파일 읽기 설정 (Encoding.Default 사용)
            // Encoding.Default는 시스템의 기본 언어 설정(한국어 윈도우의 경우 EUC-KR)을 따릅니다.
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (StreamReader sr = new StreamReader(fs, Encoding.Default))
            {
                string content = sr.ReadToEnd();
                string[] lines = content.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

                if (TryFindOffset(lines, craneNameInConfig, out float newAngle))
                {
                    if (currentAppliedAngle != newAngle)
                    {
                        currentAppliedAngle = newAngle;
                        // 값이 변경될 때만 유니티 콘솔에 로그 한 줄 남김 (확인용)
                        Debug.Log($"[Config] '{craneNameInConfig}' 각도 갱신: {newAngle}");
                    }
                }
            }
        }
        catch (Exception)
        {
            // 파일을 쓰는 중이거나 접근 불가 시 조용히 넘어감
        }
    }

    private bool TryFindOffset(string[] lines, string targetKey, out float result)
    {
        result = 0f;
        string targetSection = "[CraneBodyAngleOffset]";
        bool isInsideSection = false;

        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();

            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith(";") || trimmedLine.StartsWith("#"))
                continue;

            // 섹션 확인
            if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
            {
                isInsideSection = (trimmedLine == targetSection);
                continue;
            }

            // 섹션 안일 때 키 찾기
            if (isInsideSection)
            {
                string[] parts = trimmedLine.Split('=');
                if (parts.Length == 2)
                {
                    string key = parts[0].Trim();

                    if (key == targetKey)
                    {
                        string value = parts[1].Trim();
                        if (float.TryParse(value, out float parsedValue))
                        {
                            result = parsedValue;
                            return true; // 값을 찾으면 즉시 종료 (최적화)
                        }
                    }
                }
            }
        }
        return false;
    }
}
#if UNITY_EDITOR // 이 스크립트는 유니티 에디터에서만 컴파일되고 작동합니다.

using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class BarkLinesParser
{
    // 유니티 상단 메뉴에 "Tools/Import Bark Lines from CSV" 항목을 추가합니다.
    [MenuItem("Tools/Import Bark Lines from CSV")]
    public static void ImportBarks()
    {
        // 파일 탐색기를 열어 유저가 CSV 파일을 선택하도록 합니다.
        string path = EditorUtility.OpenFilePanel("Import Bark Lines CSV", "", "csv");
        if (string.IsNullOrEmpty(path)) return; // 파일을 선택하지 않으면 중단

        // CSV 파일의 모든 줄을 읽어옵니다.
        string[] allLines = File.ReadAllLines(path);

        // ScriptableObject 인스턴스를 메모리에 생성합니다.
        BarkLinesSO barkLinesSO = ScriptableObject.CreateInstance<BarkLinesSO>();

        List<string> linesList = new List<string>();

        // 첫 번째 줄(헤더)은 건너뛰고, 각 줄을 리스트에 추가합니다.
        for (int i = 1; i < allLines.Length; i++)
        {
            string line = allLines[i].Trim(); // 앞뒤 공백 제거
            if (!string.IsNullOrEmpty(line)) // 비어있는 줄은 무시
            {
                linesList.Add(line);
            }
        }

        // 완성된 리스트를 ScriptableObject의 배열에 할당합니다.
        barkLinesSO.lines = linesList.ToArray();

        // 저장할 경로와 파일 이름을 설정합니다.
        string fileName = Path.GetFileNameWithoutExtension(path);
        string assetPath = $"Assets/Resources/Barks/{fileName}.asset"; // 에셋을 저장할 폴더

        // 폴더가 없으면 자동으로 생성합니다.
        Directory.CreateDirectory(Path.GetDirectoryName(assetPath));

        // ScriptableObject를 에셋 파일로 저장합니다.
        AssetDatabase.CreateAsset(barkLinesSO, assetPath);
        AssetDatabase.SaveAssets();

        Debug.Log($"[BarkLinesParser] BarkLinesSO 에셋 생성 완료: {assetPath}");

        // 생성된 에셋을 프로젝트 창에서 바로 보여주고 선택해줍니다.
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = barkLinesSO;
    }
}

#endif
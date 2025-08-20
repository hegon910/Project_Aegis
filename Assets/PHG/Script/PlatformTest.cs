using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PHG
{
    public class PlatformTest : MonoBehaviour
    {

        /*전처리 문구
         * #if UNITY_EDITOR
         * 에디터 일 때, 다운로드 대신에 컴퓨터에 이미 있던 거 쓰고
         * 로그인도 Google Play Store 대신 email로 빠르게 로그인
         * 
         * #else
         *  빌드에서 테스트일 때
         *  CSV, Database 다운로드해서 쓰고
         *  로그인도 Google Play Store로 로그인
         *  
         *  #TestVersion or #ReleaseVersion 전처리 문구 작성한 뒤
         *  PlayerSettings 에서 OthersSetting -> Scripting Define Symbols에서 TestVersion 추가하면
         *  #if TestVersion 사용 가능 반대로, ReleaseVersion 추가하면 RealeaseVersion 사용 가능
         *  
         *  
        */

#if UNITY_EDITOR
        public string path = Application.dataPath;
#elif UNITY_ANDROID
        public string path = Application.persistentDataPath;
#endif


        /*
         */
        private void Start()
        {
            Debug.Log($"{path}경로");
        }



        /****안드로이드로 빌드 했을 때 모바일 환경에서 로그 확인****
         * 1. 에셋 스토어 -> 무료 에셋 중 log viewer 검색해서 설치
         * 2. 안드로이드 빌드 세팅에서 Development Build 체크, Auto Connect Profiler 체크(에디터 프로파일러로 로그 및 프로파일 확인 가능)
         * 3. 성능을 조금 포기하자면 Deep Profiling Mode 체크,
         * Script Debugging 은 코드 줄 까지 오류 확인 가능, Deep Profiling Mode 둘 다 체크하면 성능 저하
         */


    }
}


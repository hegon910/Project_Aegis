using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PHG
{
    public class PlatformTest : MonoBehaviour
    {

        /*��ó�� ����
         * #if UNITY_EDITOR
         * ������ �� ��, �ٿ�ε� ��ſ� ��ǻ�Ϳ� �̹� �ִ� �� ����
         * �α��ε� Google Play Store ��� email�� ������ �α���
         * 
         * #else
         *  ���忡�� �׽�Ʈ�� ��
         *  CSV, Database �ٿ�ε��ؼ� ����
         *  �α��ε� Google Play Store�� �α���
         *  
         *  #TestVersion or #ReleaseVersion ��ó�� ���� �ۼ��� ��
         *  PlayerSettings ���� OthersSetting -> Scripting Define Symbols���� TestVersion �߰��ϸ�
         *  #if TestVersion ��� ���� �ݴ��, ReleaseVersion �߰��ϸ� RealeaseVersion ��� ����
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
            Debug.Log($"{path}���");
        }



        /****�ȵ���̵�� ���� ���� �� ����� ȯ�濡�� �α� Ȯ��****
         * 1. ���� ����� -> ���� ���� �� log viewer �˻��ؼ� ��ġ
         * 2. �ȵ���̵� ���� ���ÿ��� Development Build üũ, Auto Connect Profiler üũ(������ �������Ϸ��� �α� �� �������� Ȯ�� ����)
         * 3. ������ ���� �������ڸ� Deep Profiling Mode üũ,
         * Script Debugging �� �ڵ� �� ���� ���� Ȯ�� ����, Deep Profiling Mode �� �� üũ�ϸ� ���� ����
         */


    }
}


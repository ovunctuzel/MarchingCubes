using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class SingleCubeTestEditor : MonoBehaviour
{
    [CustomEditor(typeof(SingleCubeTest))]
    public class TilemapPatcherEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
        
            SingleCubeTest myScript = (SingleCubeTest)target;
            if(GUILayout.Button("Regenerate"))
            {
                myScript.RegenerateCube();
            }
        }
    }
}

[RequireComponent(typeof(MeshGenerator), typeof(MeshFilter), typeof(MeshRenderer))]
public class SingleCubeTest : MonoBehaviour
{
    public bool[] activeCorners;
    public int cubeConfig = 0;

    public void RegenerateCube()
    {
        cubeConfig = 0;
        cubeConfig += activeCorners[0] ? 1 : 0;
        cubeConfig += activeCorners[1] ? 2 : 0;
        cubeConfig += activeCorners[2] ? 4 : 0;
        cubeConfig += activeCorners[3] ? 8 : 0;
        cubeConfig += activeCorners[4] ? 16 : 0;
        cubeConfig += activeCorners[5] ? 32 : 0;
        cubeConfig += activeCorners[6] ? 64 : 0;
        cubeConfig += activeCorners[7] ? 128 : 0;
        Debug.Log(cubeConfig);

        GetComponent<MeshGenerator>().cubeConfig = cubeConfig;
        GetComponent<MeshGenerator>().GenerateSingleCube();
    }

    void OnDrawGizmos()
    {
        Debug.DrawLine(new Vector3(0, 0, 0), new Vector3(0, 1, 0));
        Debug.DrawLine(new Vector3(0, 1, 0), new Vector3(1, 1, 0));
        Debug.DrawLine(new Vector3(1, 1, 0), new Vector3(1, 0, 0));
        Debug.DrawLine(new Vector3(1, 0, 0), new Vector3(0, 0, 0));

        Debug.DrawLine(new Vector3(0, 0, 1), new Vector3(0, 1, 1));
        Debug.DrawLine(new Vector3(0, 1, 1), new Vector3(1, 1, 1));
        Debug.DrawLine(new Vector3(1, 1, 1), new Vector3(1, 0, 1));
        Debug.DrawLine(new Vector3(1, 0, 1), new Vector3(0, 0, 1));

        Debug.DrawLine(new Vector3(0, 0, 1), new Vector3(0, 0, 0));
        Debug.DrawLine(new Vector3(0, 1, 1), new Vector3(0, 1, 0));
        Debug.DrawLine(new Vector3(1, 1, 1), new Vector3(1, 1, 0));
        Debug.DrawLine(new Vector3(1, 0, 1), new Vector3(1, 0, 0));
    }
}


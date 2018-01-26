﻿using System;
using System.Collections;
using System.Collections.Generic;
using Windows.Kinect;
using UnityEngine;

public class AnimationController : MonoBehaviour
{
    public KinectSensor Sensor;
    public Skeleton Skeleton;
    public SkinnedMeshRenderer ReferenceMesh;
    public MeshFilter AnimatedMesh;

    private List<Vector3> _vertices;

    void Awake()
    {
        Sensor = KinectSensor.GetDefault();
        LoadModel();
    }

    void Update()
    {
        UpdateBones();
        UpdateMesh();
    }

    //Load model from unity, map model bones to our bone structure and create a skeleton
    private void LoadModel()
    {
        //Clone the referenced mesh
        AnimatedMesh.mesh = ReferenceMesh.sharedMesh.CloneMesh();
        AnimatedMesh.GetComponent<Renderer>().materials = ReferenceMesh.GetComponent<Renderer>().materials;
        _vertices = new List<Vector3>(AnimatedMesh.mesh.vertices.Length);

        var basePoseVertices = new Vector3[ReferenceMesh.sharedMesh.vertices.Length];
        var basePoseNormals = new Vector3[ReferenceMesh.sharedMesh.normals.Length];
        var nameBoneDictionary = new Dictionary<string, BaseBone>();
        BaseBone[] boneArray = new BaseBone[0];
        var vertexIdBoneWeightDictionary = new Dictionary<int, Dictionary<string, float>>();
        int[] boneIndexArray = new int[0];
        float[] boneWeightArray = new float[0];

        //Load vertices and normals for basePose
        Array.Copy(ReferenceMesh.sharedMesh.vertices, basePoseVertices, basePoseVertices.Length);
        Array.Copy(ReferenceMesh.sharedMesh.normals, basePoseNormals, basePoseNormals.Length);

        //bones could for example be pulled from a SkinnedMeshRenderer
        boneArray = new BaseBone[ReferenceMesh.bones.Length];

        for (var i = 0; i < ReferenceMesh.bones.Length; i++)
        {
            var localPosition = ReferenceMesh.sharedMesh.bindposes[i].inverse.GetColumn(3);
            var localRotation = ReferenceMesh.sharedMesh.bindposes[i].inverse.rotation;

            if (i == 0)
            {
                boneArray[i] = new RootBone(localPosition, localRotation);
                ReferenceMesh.rootBone.name = "root";
                nameBoneDictionary.Add(ReferenceMesh.rootBone.name, boneArray[i]);
            }
            else
            {
                var parentIndex = -1;
                for (var j = 0; j < ReferenceMesh.bones.Length; j++)
                {
                    if (ReferenceMesh.bones[j] != ReferenceMesh.bones[i].parent) continue;

                    parentIndex = j;
                    break;
                }
                localRotation = (ReferenceMesh.sharedMesh.bindposes[parentIndex] * ReferenceMesh.sharedMesh.bindposes[i].inverse).rotation;
                localPosition = (ReferenceMesh.sharedMesh.bindposes[parentIndex] * ReferenceMesh.sharedMesh.bindposes[i].inverse).GetColumn(3);
                boneArray[i] = new Bone(localPosition, localRotation, ReferenceMesh.bones[i].parent.name);
                nameBoneDictionary.Add(ReferenceMesh.bones[i].name, boneArray[i]);
            }
        }

        //Unity BoneWeight class can assign up to four bones to each vertex, acessable via bone inicies
        var boneWeights = ReferenceMesh.sharedMesh.boneWeights;
        boneWeightArray = new float[basePoseVertices.Length * 4];
        boneIndexArray = new int[basePoseVertices.Length * 4];
        for (var i = 0; i < basePoseVertices.Length; i++)
        {
            Dictionary<string, float> dic = new Dictionary<string, float>();
            var name0 = ReferenceMesh.bones[boneWeights[i].boneIndex0].name;
            var name1 = ReferenceMesh.bones[boneWeights[i].boneIndex1].name;
            var name2 = ReferenceMesh.bones[boneWeights[i].boneIndex2].name;
            var name3 = ReferenceMesh.bones[boneWeights[i].boneIndex3].name;

            boneWeightArray[4 * i] = boneWeights[i].weight0;
            boneWeightArray[4 * i +1] = boneWeights[i].weight1;
            boneWeightArray[4 * i +2] = boneWeights[i].weight2;
            boneWeightArray[4 * i+3] = boneWeights[i].weight3;
            boneIndexArray[4 * i] = boneWeights[i].boneIndex0;
            boneIndexArray[4 * i + 1] = boneWeights[i].boneIndex1;
            boneIndexArray[4 * i + 2] = boneWeights[i].boneIndex2;
            boneIndexArray[4 * i + 3] = boneWeights[i].boneIndex3;

            dic.Add(name0, boneWeights[i].weight0);
            if (!dic.ContainsKey(name1))
                dic.Add(name1, boneWeights[i].weight1);
            if (!dic.ContainsKey(name2))
                dic.Add(name2, boneWeights[i].weight2);
            if (!dic.ContainsKey(name3))
                dic.Add(name3, boneWeights[i].weight3);
            vertexIdBoneWeightDictionary.Add(i, dic);
        }

        //Create a skeleton
        Skeleton = new Skeleton(nameBoneDictionary, vertexIdBoneWeightDictionary, basePoseVertices, basePoseNormals, boneArray, boneIndexArray, boneWeightArray);
    }

    //Update skeleton using kinect sensor data
    private void UpdateBones()
    {
        //TODO - do actual implementation, this is just a test 
        Skeleton.BoneIdBoneDictionary["Upper arm.R"].LocalRotation *= Quaternion.Euler(0, 45 * Time.deltaTime, 0);
        //Skeleton.BoneIdBoneDictionary["Upper arm.R"].LocalLinkPosition = new Vector3(0, 3, 0);
    }

    //Apply the bone transformations to the mesh
    private void UpdateMesh()
    {
        Vector3[] vertices, normals;

        Skeleton.UpdateVertices(out vertices, out normals);
        AnimatedMesh.mesh.vertices = vertices;
        AnimatedMesh.mesh.normals = normals;

        AnimatedMesh.mesh.RecalculateBounds();
    }
}

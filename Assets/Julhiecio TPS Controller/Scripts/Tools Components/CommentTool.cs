using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace JUTPS.Utilities
{
    [AddComponentMenu("JU TPS/Tools/Comment")]
    public class CommentTool : MonoBehaviour
    {
        [TextArea(3, 300)]
        public string Comment;
    }
}
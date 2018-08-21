using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering
{
    public static class CoreLightEditorUtilities
    {
        public struct DrawInfo
        {
            [Flags]
            public enum Draw
            {
                WireFrame = 1,
                Handle = 2,
            }

            public Draw draw;
            public Color wireFrameColor;
            public Color handleColor;
            public bool isWireFrame { get { return (draw & Draw.WireFrame) > 0; } }
            public bool isHandle { get { return (draw & Draw.Handle) > 0; } }
            public DrawInfo(Color handleColor = default(Color), Color wireFrameColor = default(Color), Draw draw = Draw.WireFrame | Draw.Handle)
            {
                this.draw = draw;
                this.wireFrameColor = wireFrameColor;
                this.handleColor = handleColor;
            }
        }

        static float SliderHandle(Vector3 position, Vector3 direction, float value)
        {
            Vector3 pos = position + direction * value;
            float sizeHandle = HandleUtility.GetHandleSize(pos);
            bool temp = GUI.changed;
            GUI.changed = false;
            position = Handles.Slider(pos, direction, sizeHandle * 0.03f, Handles.DotHandleCap, 0f);
            if (GUI.changed)
                value = Vector3.Dot(position - position, direction);
            GUI.changed |= temp;
            return value;
        }

        private static Color GetLightHandleColor(Color wireframeColor)
        {
            Color color = wireframeColor;
            color.a = Mathf.Clamp01(color.a * 2);
            return (QualitySettings.activeColorSpace == ColorSpace.Linear) ? color.linear : color;
        }

        // Don't use Handles.Disc as it break the highlight of the gizmo axis, use our own draw disc function instead for gizmo
        public static void DrawWireDisc(Quaternion q, Vector3 position, Vector3 axis, float radius)
        {
            Matrix4x4 rotation = Matrix4x4.TRS(Vector3.zero, q, Vector3.one);
            
            float theta = 0.0f;
            float x = radius * Mathf.Cos(theta);
            float y = radius * Mathf.Sin(theta);
            Vector3 pos = rotation * new Vector3(x, y, 0);
            pos += position;
            Vector3 newPos = pos;
            Vector3 lastPos = pos;
            for (theta = 0.1f; theta < 2.0f * Mathf.PI; theta += 0.1f)
            {
                x = radius * Mathf.Cos(theta);
                y = radius * Mathf.Sin(theta);

                newPos = rotation * new Vector3(x, y, 0);
                newPos += position;
                Gizmos.DrawLine(pos, newPos);
                pos = newPos;
            }
            Gizmos.DrawLine(pos, lastPos);
        }


        // innerSpotPercent - 0 to 1 value (percentage 0 - 100%)
        public static void DrawSpotlightHandle(Light spotlight, float innerSpotPercent, bool selected)
        {
            var flatRadiusAtRange = spotlight.range * Mathf.Tan(spotlight.spotAngle * Mathf.Deg2Rad * 0.5f);

            var vectorLineUp = Vector3.Normalize(spotlight.gameObject.transform.forward * spotlight.range + spotlight.gameObject.transform.up * flatRadiusAtRange);
            var vectorLineDown = Vector3.Normalize(spotlight.gameObject.transform.forward * spotlight.range + spotlight.gameObject.transform.up * -flatRadiusAtRange);
            var vectorLineRight = Vector3.Normalize(spotlight.gameObject.transform.forward * spotlight.range + spotlight.gameObject.transform.right * flatRadiusAtRange);
            var vectorLineLeft = Vector3.Normalize(spotlight.gameObject.transform.forward * spotlight.range + spotlight.gameObject.transform.right * -flatRadiusAtRange);

            var rangeDiscDistance = Mathf.Cos(Mathf.Deg2Rad * spotlight.spotAngle / 2) * spotlight.range;
            var rangeDiscRadius = spotlight.range * Mathf.Sin(spotlight.spotAngle * Mathf.Deg2Rad * 0.5f);
            var nearDiscDistance = Mathf.Cos(Mathf.Deg2Rad * spotlight.spotAngle / 2) * spotlight.shadowNearPlane;
            var nearDiscRadius = spotlight.shadowNearPlane * Mathf.Sin(spotlight.spotAngle * Mathf.Deg2Rad * 0.5f);
            
            using (new Handles.DrawingScope(Gizmos.color))
            {
                DrawCone(spotlight.gameObject.transform.position, spotlight.gameObject.transform.rotation, spotlight.range, spotlight.spotAngle * innerSpotPercent);

                if (selected)
                {
                    //Inner Cone
                    DrawCone(spotlight.gameObject.transform.position, spotlight.gameObject.transform.rotation, spotlight.range, spotlight.spotAngle);

                    //Draw Range Arcs
                    Handles.DrawWireArc(spotlight.gameObject.transform.position, spotlight.gameObject.transform.right, vectorLineUp, spotlight.spotAngle, spotlight.range);
                    Handles.DrawWireArc(spotlight.gameObject.transform.position, spotlight.gameObject.transform.up, vectorLineLeft, spotlight.spotAngle, spotlight.range);

                    //Draw Near Plane Disc
                    if (spotlight.shadows != LightShadows.None)
                        Handles.DrawWireDisc(spotlight.gameObject.transform.forward, spotlight.gameObject.transform.position + spotlight.gameObject.transform.forward * nearDiscDistance, nearDiscRadius);

                    //waiting for Martin handles
                }
            }
        }

        public static void DrawCone(Vector3 position, Quaternion rotation, float range, float angle)
        {
            var flatRadiusAtRange = range * Mathf.Tan(angle * Mathf.Deg2Rad * 0.5f);
            var forward = rotation * Vector3.forward;
            var up = rotation * Vector3.up;
            var right = rotation * Vector3.right;

            var vectorLineUp = Vector3.Normalize(forward * range + up * flatRadiusAtRange);
            var vectorLineDown = Vector3.Normalize(forward * range + up * -flatRadiusAtRange);
            var vectorLineRight = Vector3.Normalize(forward * range + right * flatRadiusAtRange);
            var vectorLineLeft = Vector3.Normalize(forward * range + right * -flatRadiusAtRange);

            //Draw Lines
            Handles.DrawLine(position, position + vectorLineUp * range);
            Handles.DrawLine(position, position + vectorLineDown * range);
            Handles.DrawLine(position, position + vectorLineRight * range);
            Handles.DrawLine(position, position + vectorLineLeft * range);
            
            if (angle > 0)
            {
                var discDistance = Mathf.Cos(Mathf.Deg2Rad * angle * 0.5f) * range;
                var discRadius = range * Mathf.Sin(angle * Mathf.Deg2Rad * 0.5f);
                Handles.DrawWireDisc(position + forward * discDistance, forward, discRadius);
            }
        }

        public static void DrawArealightGizmo(Light arealight)
        {
            var RectangleSize = new Vector3(arealight.areaSize.x, arealight.areaSize.y, 0);
            // Remove scale for light, not take into account
            var localToWorldMatrix = Matrix4x4.TRS(arealight.transform.position, arealight.transform.rotation, Vector3.one);
            Gizmos.matrix = localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, RectangleSize);
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.DrawWireSphere(arealight.transform.position, arealight.range);
        }

        [Obsolete("Should use the legacy gizmo draw")]
        public static void DrawPointlightGizmo(Light pointlight, bool selected)
        {
            if (pointlight.shadows != LightShadows.None && selected) Gizmos.DrawWireSphere(pointlight.transform.position, pointlight.shadowNearPlane);
            Gizmos.DrawWireSphere(pointlight.transform.position, pointlight.range);
        }

        // Same as Gizmo.DrawFrustum except that when aspect is below one, fov represent fovX instead of fovY
        // Use to match our light frustum pyramid behavior
        public static Vector4 DrawLightPyramidFrustumHandle(Vector3 center, Vector4 aspectMinRangeMaxRangeFov, bool useNearPlane, DrawInfo info)
        {
            if (!(info.isHandle | info.isWireFrame))
                return aspectMinRangeMaxRangeFov;

            float aspect = aspectMinRangeMaxRangeFov.x;
            float minRange = aspectMinRangeMaxRangeFov.y;
            float maxRange = aspectMinRangeMaxRangeFov.z;
            float fov = aspectMinRangeMaxRangeFov.w;
            float tanfov = Mathf.Tan(Mathf.Deg2Rad * fov * 0.5f);
            Vector3 farEnd = new Vector3(0, 0, maxRange);
            Vector3 endSizeX;
            Vector3 endSizeY;

            if (aspect >= 1.0f)
            {
                endSizeX = new Vector3(maxRange * tanfov * aspect, 0, 0);
                endSizeY = new Vector3(0, maxRange * tanfov, 0);
            }
            else
            {
                endSizeX = new Vector3(maxRange * tanfov, 0, 0);
                endSizeY = new Vector3(0, maxRange * tanfov / aspect, 0);
            }

            Vector3 s1, s2, s3, s4;
            Vector3 e1 = farEnd + endSizeX + endSizeY;
            Vector3 e2 = farEnd - endSizeX + endSizeY;
            Vector3 e3 = farEnd - endSizeX - endSizeY;
            Vector3 e4 = farEnd + endSizeX - endSizeY;

            using (new Handles.DrawingScope(info.wireFrameColor))
            {
                if (minRange <= 0.0f || !useNearPlane)
                {
                    s1 = s2 = s3 = s4 = center;
                }
                else
                {
                    Vector3 nearEnd = new Vector3(0, 0, minRange);

                    Vector3 startSizeX;
                    Vector3 startSizeY;
                    if (aspect >= 1.0f)
                    {
                        startSizeX = new Vector3(minRange * tanfov * aspect, 0, 0);
                        startSizeY = new Vector3(0, minRange * tanfov, 0);
                    }
                    else
                    {
                        startSizeX = new Vector3(minRange * tanfov, 0, 0);
                        startSizeY = new Vector3(0, minRange * tanfov / aspect, 0);
                    }
                    Vector3 startPoint = center + nearEnd;
                    s1 = startPoint + startSizeX + startSizeY;
                    s2 = startPoint - startSizeX + startSizeY;
                    s3 = startPoint - startSizeX - startSizeY;
                    s4 = startPoint + startSizeX - startSizeY;

                    if (info.isWireFrame)
                    {
                        Handles.DrawLine(s1, s2);
                        Handles.DrawLine(s2, s3);
                        Handles.DrawLine(s3, s4);
                        Handles.DrawLine(s4, s1);
                    }
                }

                if (info.isWireFrame)
                {
                    Handles.DrawLine(e1, e2);
                    Handles.DrawLine(e2, e3);
                    Handles.DrawLine(e3, e4);
                    Handles.DrawLine(e4, e1);

                    Handles.DrawLine(s1, e1);
                    Handles.DrawLine(s2, e2);
                    Handles.DrawLine(s3, e3);
                    Handles.DrawLine(s4, e4);
                }
            }
            
            if (info.isHandle)
            {
                using (new Handles.DrawingScope(info.handleColor))
                {
                    if(useNearPlane)
                    {
                        minRange = SliderHandle(center, Vector3.forward, minRange);
                    }
                    
                    maxRange = SliderHandle(center, Vector3.forward, maxRange);

                    float endHalfWidth = (e1.x - e2.x) * 0.5f;
                    float endHalfHeight = (e1.y - e4.y) * 0.5f;

                    endHalfHeight = SliderHandle(farEnd, Vector3.up, endHalfHeight);
                    endHalfHeight = SliderHandle(farEnd, Vector3.down, endHalfHeight);
                    endHalfWidth = SliderHandle(farEnd, Vector3.right, endHalfWidth);
                    endHalfWidth = SliderHandle(farEnd, Vector3.left, endHalfWidth);
                    
                    if (aspect >= 1 /*&& size.y > newSize.y*/)
                    {
                        fov = 2f * Mathf.Rad2Deg * Mathf.Atan(0.5f * endHalfHeight / maxRange);
                    }
                    else if (aspect <= 1/* && size.x > newSize.x*/)
                    {
                        fov = 2f * Mathf.Rad2Deg * Mathf.Atan(0.5f * endHalfWidth / maxRange);
                    }
                    aspect = endHalfWidth / endHalfHeight;
                }
            }

            return new Vector4(aspect, minRange, maxRange, fov);
        }

        public static void DrawLightOrthoFrustum(Vector3 center, float width, float height, float maxRange, float minRange)
        {
            Vector3 farEnd = new Vector3(0, 0, maxRange);
            Vector3 endSizeX = new Vector3(width, 0, 0);
            Vector3 endSizeY = new Vector3(0, height, 0);

            Vector3 s1, s2, s3, s4;
            Vector3 e1 = farEnd + endSizeX + endSizeY;
            Vector3 e2 = farEnd - endSizeX + endSizeY;
            Vector3 e3 = farEnd - endSizeX - endSizeY;
            Vector3 e4 = farEnd + endSizeX - endSizeY;
            if (minRange <= 0.0f)
            {
                s1 = s2 = s3 = s4 = center;
            }
            else
            {
                Vector3 startSizeX = new Vector3(width, 0, 0);
                Vector3 startSizeY = new Vector3(0, height, 0);

                Vector3 startPoint = center;
                s1 =    startPoint + startSizeX + startSizeY;
                s2 =    startPoint - startSizeX + startSizeY;
                s3 =    startPoint - startSizeX - startSizeY;
                s4 =    startPoint + startSizeX - startSizeY;
                Gizmos.DrawLine(s1, s2);
                Gizmos.DrawLine(s2, s3);
                Gizmos.DrawLine(s3, s4);
                Gizmos.DrawLine(s4, s1);
            }

            Gizmos.DrawLine(e1, e2);
            Gizmos.DrawLine(e2, e3);
            Gizmos.DrawLine(e3, e4);
            Gizmos.DrawLine(e4, e1);

            Gizmos.DrawLine(s1, e1);
            Gizmos.DrawLine(s2, e2);
            Gizmos.DrawLine(s3, e3);
            Gizmos.DrawLine(s4, e4);
        }

        [Obsolete("Should use the legacy gizmo draw")]
        public static void DrawDirectionalLightGizmo(Light directionalLight)
        {
            var gizmoSize = 0.2f;
            DrawWireDisc(directionalLight.transform.rotation, directionalLight.transform.position, directionalLight.gameObject.transform.forward, gizmoSize);
            Gizmos.DrawLine(directionalLight.transform.position, directionalLight.transform.position + directionalLight.transform.forward);
            Gizmos.DrawLine(directionalLight.transform.position + directionalLight.transform.up * gizmoSize, directionalLight.transform.position + directionalLight.transform.up * gizmoSize + directionalLight.transform.forward);
            Gizmos.DrawLine(directionalLight.transform.position + directionalLight.transform.up * -gizmoSize, directionalLight.transform.position + directionalLight.transform.up * -gizmoSize + directionalLight.transform.forward);
            Gizmos.DrawLine(directionalLight.transform.position + directionalLight.transform.right * gizmoSize, directionalLight.transform.position + directionalLight.transform.right * gizmoSize + directionalLight.transform.forward);
            Gizmos.DrawLine(directionalLight.transform.position + directionalLight.transform.right * -gizmoSize, directionalLight.transform.position + directionalLight.transform.right * -gizmoSize + directionalLight.transform.forward);
        }
    }
}

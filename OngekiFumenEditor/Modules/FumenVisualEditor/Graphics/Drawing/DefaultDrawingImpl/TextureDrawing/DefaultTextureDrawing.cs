﻿using MahApps.Metro.Controls;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using System.Windows.Media.Imaging;
using Vector2 = System.Numerics.Vector2;
using Vector3 = OpenTK.Mathematics.Vector3;
using Shader = OngekiFumenEditor.Modules.FumenVisualEditor.Graphics.Shader;
using Caliburn.Micro;

namespace OngekiFumenEditor.Modules.FumenVisualEditor.Graphics.Drawing.DefaultDrawingImpl.TextureDrawing
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class DefaultTextureDrawing : CommonDrawingBase, ITextureDrawing, IDisposable
    {
        private readonly Shader shader;
        private readonly int vertexVBO;
        private readonly int textureVBO;
        private readonly int vao;
        private IPerfomenceMonitor performenceMonitor;

        protected DefaultTextureDrawing()
        {
            performenceMonitor = IoC.Get<IPerfomenceMonitor>();
            shader = CommonSpriteShader.Shared;

            vertexVBO = GL.GenBuffer();
            textureVBO = GL.GenBuffer();

            vao = GL.GenVertexArray();

            Init();
        }

        private void Init()
        {
            GL.BindVertexArray(vao);
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, textureVBO);
                {
                    //绑定基本顶点
                    var _cacheBaseTexPos = new float[] {
                             0,0,
                             1,0,
                             1,1,
                             0,1
                    };
                    GL.BufferData(BufferTarget.ArrayBuffer, new IntPtr(sizeof(float) * _cacheBaseTexPos.Length),
                        _cacheBaseTexPos, BufferUsageHint.StaticDraw);

                    GL.EnableVertexAttribArray(0);
                    GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, sizeof(float) * 2, 0);
                }
                GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

                GL.BindBuffer(BufferTarget.ArrayBuffer, vertexVBO);
                {
                    //绑定基本顶点
                    var cacheBaseVertex = new float[] {
                            -0.5f , 0.5f ,
                             0.5f , 0.5f ,
                             0.5f , -0.5f ,
                            -0.5f , -0.5f ,
                    };
                    GL.BufferData(BufferTarget.ArrayBuffer, new IntPtr(sizeof(float) * cacheBaseVertex.Length),
                        cacheBaseVertex, BufferUsageHint.StaticDraw);

                    GL.EnableVertexAttribArray(1);
                    GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, sizeof(float) * 2, 0);
                }
                GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            }
            GL.BindVertexArray(0);
        }

        public void Dispose()
        {
            GL.DeleteVertexArray(vao);
            GL.DeleteBuffer(vertexVBO);
            GL.DeleteBuffer(textureVBO);
        }

        public void Draw(IFumenEditorDrawingContext target, Texture texture, IEnumerable<(Vector2 size, Vector2 position, float rotation)> instances)
        {
            foreach ((var size, var position, var rotation) in instances)
                Draw(target, texture, size, position, rotation);
        }

        private void Draw(IFumenEditorDrawingContext target, Texture texture, Vector2 size, Vector2 position, float rotation)
        {
            performenceMonitor.OnBeginDrawing(this);
            {
                var modelMatrix =
                    GetOverrideModelMatrix() *
                    Matrix4.CreateScale(new Vector3(texture.Width, texture.Height, 1)) *
                    Matrix4.CreateScale(new Vector3(size.X / texture.Width, size.Y / texture.Height, 1)) *
                    Matrix4.CreateRotationZ(rotation) *
                    Matrix4.CreateTranslation(position.X, position.Y, 0);

                shader.Begin();
                {
                    shader.PassUniform("Model", modelMatrix);
                    shader.PassUniform("ViewProjection", GetOverrideViewProjectMatrixOrDefault(target));
                    shader.PassUniform("diffuse", texture);

                    GL.BindVertexArray(vao);
                    {
                        GL.DrawArrays(PrimitiveType.TriangleFan, 0, 4);
                        performenceMonitor.CountDrawCall(this);
                    }
                    GL.BindVertexArray(0);
                }
                shader.End();
            }
            performenceMonitor.OnAfterDrawing(this);
        }
    }
}

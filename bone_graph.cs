using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Windows.Forms;

using PEPlugin;
using PEPlugin.Pmx;
using PEPlugin.Pmd;
using PEPlugin.SDX;
using PEPlugin.Form;
using PEPlugin.View;

namespace BoneGraph
{
    // typedefs
    // Tuple は旧式を使う
    using StringDict = Dictionary<string, string>;
    using NodeConfigDef = Tuple<NodeSifter, string>;
    using NodeConfigList = List<Tuple<NodeSifter, string>>;
    using Bone2BodiesDict = Dictionary<IPXBone, List<IPXBody>>;
    using EdgeDictValue = Tuple<NodeInfo, List<EdgeInfo>>;
    using EdgeDict = Dictionary<Node, Tuple<NodeInfo, List<EdgeInfo>>>;

    internal delegate bool NodeSifter(NodeInfo info);

    // グラフの出力形式設定(ノード)
    internal static class NodeConfig {
        // ノードの形設定
        // 上優先
        internal static NodeConfigList shapeLegend = new NodeConfigList {
            {new NodeConfigDef(
                (node) =>
                    node.hasAttr("IKボーン") && node.hasAttr("操作可能"),
                "doubleoctagon")},
            {new NodeConfigDef(
                (node) =>
                    node.hasAttr("IKボーン") && node.hasAttr("操作不可能"),
                "octagon")},
            {new NodeConfigDef(
                (node) => node.hasAttr("操作可能"), "box")},
            {new NodeConfigDef(
                (node) => node.hasAttr("操作不可能"), "ellipse")}, 
            {new NodeConfigDef(
                (node) => true, "box")}
        };

        // ノードの色設定
        internal static NodeConfigList colorLegend = new NodeConfigList {
            {new NodeConfigDef(
                (node) =>
                    node.hasAttr("IKボーン") && node.hasAttr("操作可能"),
                "orange")},
            {new NodeConfigDef(
                (node) =>
                    node.hasAttr("IKボーン") && node.hasAttr("操作不可能"),
                "yellow")},
            {new NodeConfigDef(
                (node) => node.hasAttr("物理演算"), "lightblue")},
            {new NodeConfigDef(
                (node) => true, "white")}
        };

        internal static NodeConfigList styleLegend = new NodeConfigList {
            {new NodeConfigDef(
                (node) => node.hasAttr("表示"), "solid, filled")},
            {new NodeConfigDef(
                (node) => node.hasAttr("非表示"), "dashed, filled")},
            {new NodeConfigDef(
                (node) => true, "solid, filled")}
        };

        internal static string determineShape(NodeInfo info) {
            foreach (NodeConfigDef c in shapeLegend) {
                if (c.Item1(info)) {
                    return c.Item2;
                }
            }
            // never reach
            return "box";
        }

        internal static string determineColor(NodeInfo info) {
            foreach (NodeConfigDef c in colorLegend) {
                if (c.Item1(info)) {
                    return c.Item2;
                }
            }
            // never reach
            return "white";
        }

        internal static string determineStyle(NodeInfo info) {
            foreach (NodeConfigDef c in styleLegend) {
                if (c.Item1(info)) {
                    return c.Item2;
                }
            }
            return "solid, filled";
        }
    }

    // グラフの出力形式設定(エッジ)
    internal static class EdgeConfig {
        // エッジの鏃の形の設定
        internal static StringDict shapeLegend = new StringDict {
            {"親子", "normal"},
            {"IKターゲット", "diamond"},
            {"IKリンク", "diamond"},
            {"付与親", "normal"}
        };

        // エッジの線種の設定
        internal static StringDict styleLegend = new StringDict {
            {"親子", "solid"},
            {"IKターゲット", "bold"},
            {"IKリンク", "dashed"},
            {"付与親", "dashed"}
        };

        internal static string determineShape(EdgeInfo info) {
            return shapeLegend[info.type];
        }

        internal static string determineStyle(EdgeInfo info) {
            return styleLegend[info.type];
        }
    }


    internal class EdgeInfo {
        internal Node toNode;
        internal string type;
        internal StringDict attr;

        internal EdgeInfo(Node toNode, string type) {
            this.toNode = toNode;
            this.type = type;
            attr = new StringDict();
        }

        internal void setAttr(string key, string value) {
            this.attr[key] = value;
        }

        internal bool hasAttr(string key) {
            return this.attr.ContainsKey(key);
        }

        internal string getAttr(string key) {
            return this.attr[key];
        }
    }

    internal class Node {
        internal string id;
        internal object pmxElement;

        internal Node(string id, Object element) {
            this.id = id;
            this.pmxElement = element;
        }

        public override bool Equals(object other) {
            if (null != other && other.GetType() == this.GetType()) {
                return this.id == ((Node)other).id;
            } else {
                return false;
            }
        }

        public override int GetHashCode() {
            return id.GetHashCode();
        }
    }

    internal class NodeInfo {
        internal StringDict attr;

        internal NodeInfo() {
            this.attr = new StringDict();
        }

        internal void setAttr(string key, string value) {
            this.attr[key] = value;
        }

        internal bool hasAttr(string key) {
            return this.attr.ContainsKey(key);
        }

        internal string getAttr(string key) {
            return this.attr[key];
        }
    }

    internal class BoneGraph {
        internal EdgeDict directedEdges;
        internal IEnumerable<Node> nodes {
            get { return directedEdges.Keys; }
        }

        internal BoneGraph() {
            this.directedEdges= new EdgeDict();
        }

        internal void addNode(Node node) {
            if (!this.directedEdges.ContainsKey(node)) {
                this.directedEdges[node] = new EdgeDictValue(
                    new NodeInfo(), new List<EdgeInfo>());
            }
            return;
        }

        internal void addEdge(Node fromNode, EdgeInfo info) {
            this.addNode(fromNode);
            this.addNode(info.toNode);
            this.directedEdges[fromNode].Item2.Add(info);
        }
    }

    //////////////////////////

    public class CSScriptClass: PEPluginClass
    {
        // object -> index
        private static Dictionary<IPXBone, int> boneIndexDict;
        private static Dictionary<IPXBody, int> bodyIndexDict;

        // bone -> body(s)
        private static Bone2BodiesDict bone2bodies;


        public CSScriptClass(): base() {
            m_option = new PEPluginOption(false , true ,
                "ボーン接続グラフをGraphviz形式で出力");
        }

        public override void Run(IPERunArgs args) {
            try {
                IPEPluginHost host = args.Host;
                IPEConnector connect = host.Connector;
                IPEViewConnector view = host.Connector.View;
                IPEFormConnector form = host.Connector.Form;
                IPXPmx pmx = connect.Pmx.GetCurrentState();
                plugin_main(pmx, view, form);
                connect.View.PMDView.UpdateView();
            } catch (Exception ex) {
                MessageBox.Show(
                    ex.Message , "エラー" , MessageBoxButtons.OK ,
                    MessageBoxIcon.Exclamation);
            }
        }

        private static string selectFile() {
            SaveFileDialog diag = new SaveFileDialog();
            diag.Filter = "Graphviz形式(*.dot)|*.dot";
            diag.FilterIndex = 2;
            diag.RestoreDirectory = true;
            diag.OverwritePrompt = true;
            if (diag.ShowDialog() == DialogResult.OK) {
                return diag.FileName;
            } else {
                return null;
            }
        }

        // pmx object -> index
        private static void makeIndexDict(IPXPmx pmx) {
            boneIndexDict = new Dictionary<IPXBone, int>();
            bodyIndexDict = new Dictionary<IPXBody, int>();

            for (int i = 0; i < pmx.Bone.Count; i++) {
                boneIndexDict[pmx.Bone[i]] = i;
            }
            for (int i = 0; i < pmx.Body.Count; i++) {
                bodyIndexDict[pmx.Body[i]] = i;
            }
            return;
        }

        // bone object -> body objects
        private static void makeBone2BodiesDict(IPXPmx pmx) {
            bone2bodies = new Bone2BodiesDict();
            for (int i = 0; i < pmx.Body.Count; i++) {
                IPXBody body = pmx.Body[i];
                if (null != body.Bone) {
                    if (bone2bodies.ContainsKey(body.Bone)) {
                        bone2bodies[body.Bone].Add(body);
                    } else {
                        bone2bodies[body.Bone] = new List<IPXBody> {body};
                    }
                }
            }
        }

        private static Node makeBoneNode(IPXBone bone) {
            int index = boneIndexDict[bone];
            string boneID =  "BONE_" + index.ToString();
            return new Node(boneID, bone);
        }

        private static BoneGraph makeBoneGraph(IPXPmx pmx) {
            BoneGraph boneGraph = new BoneGraph();
            foreach (IPXBone bone in pmx.Bone) {
                Node boneNode = makeBoneNode(bone);
                ///////////
                // 4 種類のエッジを作成する
                // (1) 親 -> 子 (type=親子)
                // (2) 付与親 -> 子 (type=付与親)
                // (2) IKボーン -> IKターゲットボーン (type=IKターゲット)
                // (3) IKボーン -> IKリンクボーン (type=IKリンク)

                boneGraph.addNode(boneNode); // 4種に当てはまらないボーン用
            
                // (1)
                IPXBone parentBone = bone.Parent;
                if (null != parentBone) {
                    Node parentNode = makeBoneNode(parentBone);
                    EdgeInfo info = new EdgeInfo(boneNode, "親子");
                    boneGraph.addEdge(parentNode, info);
                }
                // (2)
                if (null != bone.AppendParent &&
                        (bone.IsAppendRotation || bone.IsAppendTranslation)) {
                    Node appendNode = makeBoneNode(bone.AppendParent);
                    EdgeInfo info = new EdgeInfo(boneNode, "付与親");
                    info.setAttr(
                        "weight",
                        string.Format("{0:F3}", bone.AppendRatio));
                    boneGraph.addEdge(appendNode, info);
                }
                // (3), (4)
                if (bone.IsIK && null != bone.IK) {
                    if (null != bone.IK.Target) {
                        Node targetNode = makeBoneNode(bone.IK.Target);
                        EdgeInfo targetInfo = new EdgeInfo(
                            targetNode, "IKターゲット");
                        boneGraph.addEdge(boneNode, targetInfo);
                    }
                    foreach (IPXIKLink ikLink in bone.IK.Links) {
                        Node linkNode = makeBoneNode(ikLink.Bone);
                        EdgeInfo linkInfo = new EdgeInfo(
                            linkNode, "IKリンク");
                        boneGraph.addEdge(boneNode, linkInfo);
                    }
                }
            }
            return boneGraph;
        }

        // ボーン(ノード)の属性を設定する
        private static void updateNodeAttrs(BoneGraph g) {
            foreach (Node node in g.nodes) {
                NodeInfo info = g.directedEdges[node].Item1;
                IPXBone bone = (IPXBone)node.pmxElement;
                if (bone.IsIK) {
                    info.setAttr("IKボーン", "");
                }
                if (bone.Controllable) {
                    info.setAttr("操作可能", "");
                } else {
                    info.setAttr("操作不可能", "");
                }
                if (bone.Visible) {
                    info.setAttr("表示", "");
                } else {
                    info.setAttr("非表示", "");
                }
                if (bone2bodies.ContainsKey(bone)) {
                    string bodytext = "";
                    bool first = true;
                    bool dyn = false;
                    foreach (IPXBody body in bone2bodies[bone]) {
                        int bodyIndex = bodyIndexDict[body];
                        if (!first) {
                            bodytext += ", ";
                        }
                        bodytext += 
                            String.Format("{0:D}: {1}", bodyIndex, body.Name);
                        if (BodyMode.Dynamic == body.Mode ||
                                BodyMode.DynamicWithBone == body.Mode) {
                            dyn = true;
                        }
                        first = false;
                    }
                    if (dyn) {
                        info.setAttr("物理演算", bodytext);
                    } else {
                        info.setAttr("ボーン追従", bodytext);
                    }
                }
            }
        }

        private static StreamWriter makeWriter() {
            string dotFileName = selectFile();
            if (null == dotFileName) {
                throw new System.Exception("ファイルを選択してください");
            }
            return new StreamWriter(
                new FileStream(dotFileName, FileMode.Create),
                new UTF8Encoding(false)); // without BOM
        }

        private static void writeHeader(StreamWriter writer) {
            writer.WriteLine("digraph Bone_Graph {");
            writer.WriteLine("graph [charset = \"UTF-8\"];");
            writer.WriteLine(
                "node[fontname=\"meiryo\", fillcolor=\"white\"];");
        }

        private static void writeFooter(StreamWriter writer) {
            writer.WriteLine("}");
        }

        private static void writeBoneNode(
                Node node, NodeInfo info, StreamWriter writer) {
            IPXBone bone = (IPXBone)node.pmxElement;
            string shape = NodeConfig.determineShape(info);
            string color = NodeConfig.determineColor(info);
            string style = NodeConfig.determineStyle(info);

            int index = boneIndexDict[bone];
            string nodeLabel = string.Format("{0:D}: {1}", index, bone.Name);
            string bodyLabel = null;
            if (info.hasAttr("物理演算")) {
                bodyLabel = info.getAttr("物理演算");
            } else if (info.hasAttr("ボーン追従")) {
                bodyLabel = info.getAttr("ボーン追従");
            }
            string label = null == bodyLabel ?
                "\"" + nodeLabel + "\"":
                "\"" + nodeLabel + "\\n(" + bodyLabel + ")\"";
            writer.WriteLine(string.Format(
                "{0} [shape={1}, label={2}, style =\"{3}\"," +
                "fillcolor=\"{4}\"];",
                node.id, shape, label, style, color));
        }

        private static void writeNode(
                Node node, NodeInfo info, StreamWriter writer) {
            if (node.id.StartsWith("BONE_")) {
                writeBoneNode(node, info, writer);
            } else {
                throw new System.Exception("bug!");
            }
        }

        private static void writeEdge(
                Node fromNode, List<EdgeInfo> edgeInfos, StreamWriter writer) {
            foreach (EdgeInfo info in edgeInfos) {
                string shape = EdgeConfig.determineShape(info);
                string style = EdgeConfig.determineStyle(info);
                string modifier = string.Format(
                    "style=\"{0}\", arrowhead=\"{1}\"", style, shape);
                if (info.hasAttr("weight")) {
                    modifier += string.Format(
                        ", headlabel=\"{0}\"", info.getAttr("weight"));
                }
                writer.WriteLine(string.Format(
                    "{0} -> {1} [{2}];",
                    fromNode.id, info.toNode.id, modifier));
            }
        }

        private static void toDot(BoneGraph g, StreamWriter writer) {
            IEnumerable<Node> printNodes = g.nodes;
            writeHeader(writer);
            EdgeDict edges = g.directedEdges;
            foreach (Node node in printNodes) {
                writeNode(node, edges[node].Item1, writer);
            }
            foreach (Node node in printNodes) {
                writeEdge(node, edges[node].Item2, writer);
            }
            writeFooter(writer);
        }

        private static void plugin_main(
                IPXPmx pmx, IPEViewConnector view, IPEFormConnector form) {

            StreamWriter writer = makeWriter();
            using (writer) {
                makeIndexDict(pmx);
                makeBone2BodiesDict(pmx);
                BoneGraph g = makeBoneGraph(pmx);
                updateNodeAttrs(g);
                toDot(g, writer);
            }
            return;
        }
    }
}

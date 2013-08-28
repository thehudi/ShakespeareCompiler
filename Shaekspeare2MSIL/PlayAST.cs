﻿using Irony.Ast;
using Irony.Interpreter.Ast;
using Irony.Parsing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Shakespeare.Utility;
using TriAxis.RunSharp;

namespace Shakespeare.AST
{
    internal static class Constant
    {
        public static readonly int COMMENT_COLUMN = 40;
    }


    internal static class Extensions
    {
        const string keyAG = "AssemblyGen";
        public static AssemblyGen AG (this AstContext context)
        {
            Object obj;
            if (context.Values.TryGetValue(keyAG, out obj))
            {
                return obj as AssemblyGen;
            }

            var ag = new AssemblyGen("output.exe", true);
            context.Values.Add(keyAG, ag);
            return ag;
        }
    }


    public class PlayNode : ShakespeareBaseAstNode
    {

        protected override object ReallyDoEvaluate(Irony.Interpreter.ScriptThread thread)
        {
            var ag = AstContext.AG();
            TypeGen ScriptClass = ag.Public.Class("Shakespeare.Program.Script", typeof(Shakespeare.Support.Dramaturge));

            TypeGen MyClass = ag.Public.Class("Shakespeare.Program.Program");
            CodeGen g = MyClass.Public.Static.Method(typeof(void), "Main").Parameter(typeof(string[]), "args");
            var script = g.Local(Exp.New(ScriptClass.GetCompletedType()));
            g.Invoke(script, "Action");

            CodeGen gg = ScriptClass.Public.Constructor().
            sw.WriteLine("\t\tclass Script : Dramaturge");
            sw.WriteLine("\t\t{");
            sw.WriteLine();
            sw.WriteLine("\t\tpublic Script()");
            sw.WriteLine("\t\t : base(Console.In, Console.Out)");
            sw.WriteLine("\t\t{ }");
            sw.WriteLine();
            sw.WriteLine("\t\tpublic void Action()");
            sw.WriteLine("\t\t{");
            (TreeNode.AstNode as ShakespeareBaseAstNode).OutputTo(sw);
            sw.WriteLine("\t\t}");
            sw.WriteLine("\t}");
            sw.WriteLine("}");
            sw.Flush();
            Context.UnderlyingStream.Position = 0;
            using (var sr = new StreamReader(Context.UnderlyingStream))
            {
                return sr.ReadToEnd();
            }


        }
        public override ShakespeareBaseAstNode OutputTo(TextWriter tw)
        {
            AstNode1.OutputTo(tw);  // Title
            tw.WriteLine();
            var cdl = AstNode2 as CharacterDeclarationListNode;
            foreach (var ch in cdl.Characters)
                ch.OutputTo(tw);

            tw.WriteLine();
            AstNode3.OutputTo(tw);

            return this;
        }

        public override string ToString()
        {
            return "Play";
        }
    }

    public class TitleNode : ShakespeareBaseAstNode
    {
        public override ShakespeareBaseAstNode OutputTo(TextWriter tw)
        {
            tw.WriteLine("/********************************************************************");
            tw.WriteLine("*");
            tw.WriteLine("{0,64}", Child1.Token.Text.ToUpper());
            tw.WriteLine("*");
            tw.WriteLine("*********************************************************************/");
            return this;
        }
    }

    public class CharacterDeclarationListNode : ListNode
    {
        public List<CharacterDeclarationNode> Characters { get; set; }
        public override void Init(AstContext context, Irony.Parsing.ParseTreeNode treeNode)
        {
            base.Init(context, treeNode);
            Characters = treeNode.ChildNodes.Select(cn => cn.AstNode as CharacterDeclarationNode).ToList();
        }
    }

    public class CharacterListNode : ShakespeareBaseAstNode
    {
        public override void Init(AstContext context, ParseTreeNode treeNode)
        {
            base.Init(context, treeNode);
            //if (treeNode.ChildNodes.Count > 2)
            //    context.AddMessage(Irony.ErrorLevel.Error, Location, "Too Many Characters in scene.");
            //else if (Exist1 && !Context.Characters.Any(bnf=> bnf.Name == String1))
            //        context.AddMessage(Irony.ErrorLevel.Error, Location, @"Character ""{0}"" was not listed in the character list", String1);
            //else if (Exist2 && !Context.Characters.Any(bnf=> bnf.Name == String2))
            //        context.AddMessage(Irony.ErrorLevel.Error, Location, @"Character ""{0}"" was not listed in the character list", String2);
            //else
            //{
            //    if (Exist1)
            //        Context.ActiveCharacter1 = String1;
            //    if (Exist2)
            //        Context.ActiveCharacter2 = String2;

            //}
        }
        public void Fill(ICollection<CharacterNode> coll)
        {
            foreach (CharacterNode cn in ChildNodes)
            {
                if (cn != null)
                    coll.Add(cn);
            }
        }
    }

    public class ActHeaderNode : ShakespeareBaseAstNode
    {
        string actnumber;

        public override void Init(AstContext context, ParseTreeNode treeNode)
        {
            base.Init(context, treeNode);
            actnumber = String1.str2varname();
        }

        public override ShakespeareBaseAstNode OutputTo(TextWriter tw)
        {
            Context.CurrentAct = actnumber;

            tw.WriteLine();
            tw.Write((Context.CurrentAct+":").PadRight(Constant.COMMENT_COLUMN));
            tw.WriteLine(AstNode2);

            return this;
        }
    }

    public class EnterNode : ShakespeareBaseAstNode
    {
        public override void Init(AstContext context, ParseTreeNode treeNode)
        {
            base.Init(context, treeNode);
            if (!Exist1)
            {
                context.AddMessage(Irony.ErrorLevel.Error, Location, @"""Enter"" missing character list");
                return;
            }
        }
        public override ShakespeareBaseAstNode OutputTo(TextWriter sw)
        {
            var cn = TreeNode.ChildNodes;
            if (Child1.AstNode is CharacterListNode)
                cn = Child1.ChildNodes;

            foreach (var ch in cn)
            {
                sw.WriteLine("\t\tEnterScene({0}, {1});", Location.Line, ch.AstNode.ToString().str2varname());
                Context.ActiveCharacters.Add(ch.AstNode as CharacterNode);
            }
            return this;
        }
    }

    public class ExitNode : ShakespeareBaseAstNode
    {
        public override ShakespeareBaseAstNode OutputTo(TextWriter tw)
        {
            var charList = new List<CharacterNode>();
            if (Exist1)
            {
                if (AstNode1 is CharacterNode)
                    charList.Add(AstNode1 as CharacterNode);
                else
                    (AstNode1 as CharacterListNode).Fill(charList);

                foreach (var chr in charList)
                {
                    tw.WriteLine("\t\tExitScene({0}, {1});", Location.Line, chr);
                    Context.ActiveCharacters.Remove(chr);
                }
            }
            else
            {
                tw.WriteLine("\t\tExitSceneAll({0});", Location.Line);
                Context.ActiveCharacters.Clear();
            }

            return this;
        }
    }



    public class LineNode : ShakespeareBaseAstNode
    {
        public override ShakespeareBaseAstNode OutputTo(TextWriter tw)
        {
            tw.WriteLine();
            tw.WriteLine("\t\tActivate({0}, {1});", Location.Line, AstNode1);
            AstNode2.OutputTo(tw);
            return this;
        }
    }

    public class SentenceNode : ShakespeareBaseAstNode
    {
        public override ShakespeareBaseAstNode OutputTo(TextWriter tw)
        {
            if (AstNode1 is UnconditionalSentenceNode)
                AstNode1.OutputTo(tw);
            else
            {
                tw.WriteLine("\t\tif({0}) {{", AstNode1);
                AstNode2.OutputTo(tw);
                tw.WriteLine("\t\t}");
            }

            return this;
        }
    }

    public class UnconditionalSentenceNode : SelfNode   {}


    public class InOutNode : ShakespeareBaseAstNode
    {
        public override ShakespeareBaseAstNode OutputTo(TextWriter tw)
        {
            if (AstNode1 is OpenYourNode)
            {
                if (String2 == "heart (Keyword)")
                {
                    tw.WriteLine("\t\tIntOutput({0});", Location.Line);
                }
                else  // Open Your Mind
                {
                    tw.WriteLine("\t\tCharInput({0});", Location.Line);
                }
            }
            else if (String1 == "speak")
            {
                tw.WriteLine("\t\tCharOutput({0});", Location.Line);
            }
            else if (String1 == "listen to")
            {
                    tw.WriteLine("\t\tIntInput({0});", Location.Line);
            }
            return this;
        }
    }

    public class JumpNode : ShakespeareBaseAstNode
    {
        public override ShakespeareBaseAstNode OutputTo(TextWriter tw)
        {
            if (AstNode2 is SceneRomanNode)
                tw.WriteLine("\t\tgoto {0}_{1};", Context.CurrentAct, AstNode2.ToString().str2varname());
            else
                tw.WriteLine("\t\tgoto {0};", AstNode2.ToString().str2varname());

            return this;
        }

    }

    public class QuestionNode : ShakespeareBaseAstNode
    {
        public override ShakespeareBaseAstNode OutputTo(TextWriter tw)
        {
            tw.WriteLine("\t\tComp1 = {0};", AstNode2);
            tw.WriteLine("\t\tComp2 = {0};", AstNode4);
            tw.WriteLine("\t\tTruthFlag = {0};", AstNode3);

            return this;
        }
    }

    public class RecallNode : ShakespeareBaseAstNode
    {
        public override ShakespeareBaseAstNode OutputTo(TextWriter tw)
        {
            tw.WriteLine("\t\tPop({0});", Location.Line);
            return this;
        }
    }

    public class RememberNode : ShakespeareBaseAstNode
    {
        public override ShakespeareBaseAstNode OutputTo(TextWriter tw)
        {
            tw.WriteLine("\t\tPush({0}, {1});", Location.Line, AstNode2);

            return this;
        }
    }

    public class StatementNode : ShakespeareBaseAstNode
    {
        public override ShakespeareBaseAstNode OutputTo(TextWriter tw)
        {
            if (AstNode1 is SecondPersonNode)
            {
                if (AstNode2 is BeNode)
                {
                    if (AstNode3 is ConstantNode)
                        tw.WriteLine("\t\tAssign({0}, {1});", Location.Line, AstNode3);
                    else    // SECOND_PERSON BE Equality Value StatementSymbol 
                        tw.WriteLine("\t\tAssign({0}, {1});", Location.Line, AstNode4);
  
                }
                else if (AstNode2 is UnarticulatedConstantNode)
                {
                    tw.WriteLine("\t\tAssign({0}, {1});", Location.Line, AstNode2);

                }
            }

            return this;
        }

    }

    public class ConstantNode : ShakespeareBaseAstNode
    {
        public override ShakespeareBaseAstNode OutputTo(TextWriter tw)
        {
            if (AstNode1 is NothingNode)
                tw.Write("0");
            else  // astNode1 is Article, FirstPErson, SecondPerson, thirdPerson
                AstNode2.OutputTo(tw);

            return this;
        }
    }

    public class SceneHeaderNode : ShakespeareBaseAstNode
    {
        string scenenumber;

        public override void Init(AstContext context, ParseTreeNode treeNode)
        {
            base.Init(context, treeNode);
            scenenumber = String1.str2varname();
        }

        public override ShakespeareBaseAstNode OutputTo(TextWriter tw)
        {
            Context.CurrentScene = Context.CurrentAct + "_" + scenenumber;
            tw.WriteLine("{0}{1}", (Context.CurrentScene + ":").PadRight(Constant.COMMENT_COLUMN), AstNode2);
            return this;
        }
    }

    public class SceneContentsNode : ListNode  {  }

    public class SceneNode : TwoPartNode { }

    public class NegativeConstantNode : ShakespeareBaseAstNode
    {
        public override ShakespeareBaseAstNode OutputTo(TextWriter tw)
        {
            if (AstNode1 is NegativeNounNode)
                tw.Write("(-1)");
            else // astnode1 is NegativeAdjective or astnode1 is neutralAdjective
                tw.Write("2*{0}", AstNode2);
            return this;
        }
    }

    public class NonnegatedComparisonNode : ShakespeareBaseAstNode
    {
        public override ShakespeareBaseAstNode OutputTo(TextWriter tw)
        {
            tw.Write("({0})", AstNode1);
            return this;
        }
    }



    public class CharacterDeclarationNode : ShakespeareBaseAstNode 
    {
        public string Declaration { get; set; }

        public override ShakespeareBaseAstNode OutputTo(TextWriter tw)
        {
            tw.WriteLine("\t\tCharacter\t{0} = InitializeCharacter({2},\"{0}\");\t\t{1}", AstNode1, AstNode2, Location.Line);
            return this;
        }
    }

    public class CommentNode :ShakespeareBaseAstNode
    {
        public override string ToString()
        {
            return string.Format("/* {0} */", TreeNode.Token.Text);
        }
    }

    public class ActNode : TwoPartNode { }

    public class ComparisonNode : ShakespeareBaseAstNode 
    {
        public override ShakespeareBaseAstNode OutputTo(TextWriter tw)
        {
            if (AstNode1 is NonnegatedComparisonNode)
                AstNode1.OutputTo(tw);
//                tw.WriteLine(AstNode1);
            else
                tw.WriteLine("!{0}", AstNode2);
            return this;
        }
    }

    public class ConditionalNode :ShakespeareBaseAstNode
    {
        public override string ToString()
        {
            if (Child1.Term.Name == "if so")
                return "TruthFlag";
            else
                return "!TruthFlag";   // if not
        }
    }

    public class ValueNode : ShakespeareBaseAstNode
    {
        public override ShakespeareBaseAstNode OutputTo(TextWriter tw)
        {
            if (AstNode1 is CharacterNode)
            {
                tw.Write(AstNode1);
                tw.Write(".Value");
            }
            else if (AstNode1 is ConstantNode)
                AstNode1.OutputTo(tw);
            else if (AstNode1 is PronounNode)
            {
                tw.Write("ValueOf({0},{1})", Location.Line, AstNode1);
            }
            else if (AstNode1 is BinaryOperatorNode)
            {
                tw.Write(AstNode1.ToString(),Location.Line,  AstNode2, AstNode3);
            }
            else if (AstNode1 is UnaryOperatorNode)
            {
                AstNode1.OutputTo(tw);
                tw.Write((AstNode1 as UnaryOperatorNode).FormatString, AstNode2);
            }
            else
            {
                // error
            }
            return this;
        }

    }

    public class PronounNode : ShakespeareBaseAstNode
    {
        public override string ToString()
        {
            if (AstNode1 is FirstPersonNode || AstNode1 is FirstPersonReflexiveNode)
                return "FirstPerson";
            else if (AstNode1 is SecondPersonNode || AstNode1 is SecondPersonReflexiveNode)
                return "SecondPerson";
            return "ERROR";
        }
        
    }

    public class PositiveConstantNode : ShakespeareBaseAstNode
    {
        public override ShakespeareBaseAstNode OutputTo(TextWriter tw)
        {
            if (AstNode1 is PositiveNounNode)
                tw.Write("1");
            else
            {
                tw.Write("2*{0}", AstNode2);
            }
            return this;
        }
    }

    public class EqualityNode : ShakespeareBaseAstNode
    {
        public override ShakespeareBaseAstNode OutputTo(TextWriter tw)
        {
            tw.Write("Comp1 == Comp2");
            return this;
        }
    }

    public class ComparativeNode : ShakespeareBaseAstNode
    {
        public override ShakespeareBaseAstNode OutputTo(TextWriter tw)
        {
            if (AstNode1 is NegativeComparativeNode)
                tw.Write("Comp1 < Comp2");
            else  // PositiveComparativeNode
                tw.Write("Comp1 > Comp2");
            return this;
        }
    }

    public class NegativeComparativeNode : ShakespeareBaseAstNode
    {
        public override ShakespeareBaseAstNode OutputTo(TextWriter tw)
        {
            if (AstNode1 is NegativeComparativeTermNode)
                AstNode1.OutputTo(tw);
            else
                AstNode2.OutputTo(tw);

            return this;
        }
    }

    public class PositiveComparativeNode : ShakespeareBaseAstNode
    {
        public override ShakespeareBaseAstNode OutputTo(TextWriter tw)
        {
            var term = AstNode1.Term;
            var strTerm = term != null ? term.ToString() : null;
            if (strTerm == "more" || strTerm == "less")
            {
                AstNode1.OutputTo(tw);
                tw.Write(' ');
                AstNode2.OutputTo(tw);
            }
            else
                AstNode1.OutputTo(tw);

            return this;
        }
    }

    public class BinaryOperatorNode : ShakespeareBaseAstNode
    {
        string format;

        public override void Init(AstContext context, ParseTreeNode treeNode)
        {
            base.Init(context, treeNode);
            var term = Child1.Term.Name;
            if (term == "the difference between")
                format = "(({1})-({2}))";
            else if (term == "the product of")
                format = "(({1})*({2}))";
            else if (term == "the quotient between")
                format = "(({1})/({2}))";
            else if (term == "the remainder of the quotient between")
                format = "(({1})%({2}))";
            else if (term == "the sum of")
                format = "(({1})+({2}))";
        }

        public override ShakespeareBaseAstNode OutputTo(TextWriter tw)
        {
            tw.Write(format, Location.Line, AstNode2, AstNode3);
            return this;
        }

        public override string ToString()
        {
            return format;
        }
    }

    public class UnaryOperatorNode : ShakespeareBaseAstNode
    {
        static readonly Dictionary<string, string> functionMap = new Dictionary<string, string>
        {
                {"the cube of", "Cube"},
                {"the factorial of", "Factorial"},
                {"the square of", "Square"},
                {"the square root of", "Sqrt"},
                {"twice", "Twice"},
        };

        public string FormatString { get; set; }

        public override ShakespeareBaseAstNode OutputTo(TextWriter tw)
        {
            FormatString = string.Format("{0}({1},{{0}})", functionMap[String1], Location.Line);
            return this;
        }
    }



}

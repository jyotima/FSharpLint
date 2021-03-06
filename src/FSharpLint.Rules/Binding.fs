﻿(*
    FSharpLint, a linter for F#.
    Copyright (C) 2014 Matthew Mcveigh

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*)

namespace FSharpLint.Rules

module Binding =
    
    open Microsoft.FSharp.Compiler.Ast
    open Microsoft.FSharp.Compiler.Range
    open Microsoft.FSharp.Compiler.SourceCodeServices
    open FSharpLint.Framework.Ast
    open FSharpLint.Framework.Configuration
    open FSharpLint.Framework.LoadVisitors

    [<Literal>]
    let AnalyserName = "FSharpLint.Binding"

    let isRuleEnabled config ruleName =
        match isRuleEnabled config AnalyserName ruleName with
            | Some(_) -> true
            | None -> false
            
    /// Checks if any code uses 'let _ = ...' and suggests to use the ignore function.
    let checkForBindingToAWildcard visitorInfo pattern range =
        if "FavourIgnoreOverLetWild" |> isRuleEnabled visitorInfo.Config then
            let rec findWildAndIgnoreParens = function
                | SynPat.Paren(pattern, _) -> findWildAndIgnoreParens pattern
                | SynPat.Wild(_) -> true
                | _ -> false
                
            if findWildAndIgnoreParens pattern then
                visitorInfo.PostError range (FSharpLint.Framework.Resources.GetString("RulesFavourIgnoreOverLetWildError"))

    let checkForUselessBinding visitorInfo pattern expr range =
        if "UselessBinding" |> isRuleEnabled visitorInfo.Config then
            let rec findBindingIdentifier = function
                | SynPat.Paren(pattern, _) -> findBindingIdentifier pattern
                | SynPat.Named(_, ident, _, _, _) -> Some(ident)
                | _ -> None

            let rec exprIdentMatchesBindingIdent (bindingIdent:Ident) = function
                | SynExpr.Paren(expr, _, _, _) -> 
                    exprIdentMatchesBindingIdent bindingIdent expr
                | SynExpr.Ident(ident) ->
                    ident.idText = bindingIdent.idText
                | _ -> false
                
            findBindingIdentifier pattern |> Option.iter (fun bindingIdent ->
                if exprIdentMatchesBindingIdent bindingIdent expr then
                    visitorInfo.PostError range (FSharpLint.Framework.Resources.GetString("RulesUselessBindingError")))
    
    let visitor visitorInfo checkFile astNode = 
        match astNode.Node with
            | AstNode.Binding(SynBinding.Binding(_, _, _, _, _, _, _, pattern, _, expr, range, _)) -> 
                checkForBindingToAWildcard visitorInfo pattern range
                checkForUselessBinding visitorInfo pattern expr range
            | _ -> ()

        Continue

    type RegisterBindingVisitor() = 
        let plugin =
            {
                Name = AnalyserName
                Visitor = Ast(visitor)
            }

        interface IRegisterPlugin with
            member this.RegisterPlugin with get() = plugin
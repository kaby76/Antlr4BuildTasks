parser grammar RootParser;
options { tokenVocab=TokenLexer; }
import BaseParser;
root : base EOF;

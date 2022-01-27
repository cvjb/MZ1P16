#! /usr/bin/gforth
\ 
\ Projekt: Plottersteuerung für den MZ-1P16 (SHARP Electr. GmbH/SHARP Corp.)
\ Name   : MZ1P16
\ Zweck  : Plotterbefehle definieren.
\ Autor  : Christian V. J. Brüssow
\ Stand  : Sam 10 Jul 1999 19:18:39 MEST
\ Notiz  : BETA
\          !!! Bisher nur auf dem originalen MZ-821 unter MZ-FORTH getestet !!!
\ 

\ In ANS-/gforth unbekannte Wörter, die im Programm verwendet werden:

\ ASCII Codes von Zeichen generieren.
: ASCII	( -- c )	\ Execute-Modus
	( -- )		\ Compile-Modus: compiliert als Literal
	32	\ mit ASCII 32 (Leerzeichen) als Trennzeuchen
	WORD	\ separiere im Eingabestrom das nächste Wort als
		\ Count-String. Übergib die Adresse des Count-Bytes.
	1+ C@	\ greife auf das erste Zeichen im String zu
	STATE @	\ und ... je nachdem ...
	IF
		POSTPONE LITERAL	\ compiliere als Literal,
	ENDIF				\ oder übergib auf dem TOS.
; IMMEDIATE	\ auch im Compiler-Modus aktiv sein.

\ Abgewandeltes CR: nur Wagenrücklauf (Steuercode 0x0D)
\ Das MZ-FORTH eigene CR' erhöht eine interne Variable OUT
\ um einen Wert, der sich aus modulo COL# (eine weitere,
\ interne Variable) ergibt. Dies dient nur Formatierungs-
\ zwecken. COL# ist in MZ-FORTH gleich der Anzahl Zeichen
\ pro Zeile, des Ausgabemediums.
: CR'	( -- )	13 EMIT ;

\ Kopiert die oberste Zahl des Returnstacks auf den TOS.
: R	( -- n )	R> DUP >R ;

\ (LINE) liefert die Startadresse a der Zeile n1 des Screens n2, sowie
\ deren Länge l (bei MZ-Forth immer 64). Die Zeilen zählen von 0 bis 15.
: (LINE)	( n1 n2 -- a l )
	BUFFER		\ Anfangsadresse des Screens mit der Nummer n2
	SWAP 64 *	\ n1 * 64 Bytes weiter fängt die Zeile n1 an
	+		\ Anfangsadresse der Zeile n2 in n1
	64 ;		\ Länge der Zeile (immer 64)

\ Dummy für die MZ-FORTH Variable PROTOCOL.
0 VARIABLE PROTOCOL

\ Dummy für die MZ-FORTH Variable OUTDEV.
0 VARIABLE OUTDEV


\ MZ-1P16 Vokabular
VOCABULARY MZ1P16 IMMEDIATE
MZ1P16 DEFINITIONS

0 VARIABLE Farbe	\ enthält die aktuelle Farbnummer

: +Farbe	( -- )	29 EMIT ;	\ Farbkopf um eine Position weiterdrehen

: setzen ( b a -- )	\ neue Farbe einstellen
	OVER SWAP 0 2DUP =
	IF 2DROP DROP			\ neue Farbe = alte Farbe => nichts tun
	ELSE - DUP 0< IF 4 + ENDIF	\ Anzahl der 1/4-Drehungen des Farbkopfes
	     0 DO +Farbe LOOP		\ Kopf auf neue Farbe drehen
		  Farbe !		\ neue Farbe merken
	ENDIF
;

\ Farbzuweisungen:
: schwarz	0 Farbe setzen ;
: blau		1 Farbe setzen ;
: gruen		2 Farbe setzen ;
: rot			3 Farbe setzen ;


: Text		( -- )	1 emit ;

: 0Plotter	\ Reset des Plotters mit Farbstift-Test
	Text 4 EMIT ;


9 CONSTANT kleine	\ 80 Zeichen pro Zeile
11 CONSTANT grosse	\ 40 Zeichen pro Zeile
12 CONSTANT riesige	\ 26 Zeichen pro Zeile. Achtung: Steuercode ist
			\ eigentlich gleich "11", der angegebene Wert dient
			\ nur zur Unterscheidung zu "grosse". Die
			\ entsprechende Auswertung findet in "Schriftart" statt.
: Schriftart	( u -- )
	DUP 12 <> IF 9 EMIT 9 EMIT		\ klein u. gross brauchen diese "Vorgabe"
				 ELSE DROP 11	\ riesig nicht, muß aber gleich 11 sein.
				 THEN
	EMIT ;

: LF		( n -- )		\ Zeilenvorschübe, es sind auch negative Werte erlaubt!
	Text
	DUP
	0< IF 3
		ELSE 13
		THEN SWAP
	ABS 0 DO DUP EMIT LOOP DROP ;

: FF		( u -- )		\ Seitenvorschub. Der Zeilenzähler wird auf 0 gesetzt.
	16 EMIT ;

: L/P		( b -- )		\ Zeilen pro Seite (max. erlaubt sind 255)
	9 EMIT 9 EMIT
	ABS 255 AND >R						\ ungültige Werte unterdrücken
	R 100 / [ ASCII 0 ] LITERAL + EMIT			\ 100er
	R  10 / R 100 / 10 * - [ ASCII 0 ] LITERAL + EMIT	\ 10er
	R R> 10 / 10 * - [ ASCII 0 ] LITERAL + EMIT		\ 1er
	13 EMIT
;


0 VARIABLE TAB		\ Tabulatorwert bei Textausgabe

: TAB!	( b -- )	\ b = neuer Tabulatorwert
	ABS TAB ! ;

ASCII ; VARIABLE (ENDE)	\ Eingabe-Endkennung

: Ende!	( c -- )			\ c = ASCII-Wert der neuen Endkennung
	127 AND (ENDE) ! ;

: Ende	( -- c )			\ Endkennung auf den Stack legen
	(ENDE) @ ;

: (.Text)	( u -- f )	\ Unterroutine zur Textausgabe mit ".Text"
	16 0 DO	I OVER (LINE)
				OVER C@ ENDE =		\ Endkennung erreicht?
				IF  2DROP DROP 0 LEAVE	\ Ja => Ausgabe beenden
				ELSE	TAB @ SPACES	\ Tabulator
						-TRAILING TYPE CR	\ Zeile ausgeben
				ENDIF
		  LOOP ;

\ Textausgabe über mehrere Screens, bis die Endkennung (Vorgabe: ";")
\ oder eine ungültige Screennummer erreicht wird. Im letzteren Fall
\ erfolgt Abbruch der Ausgabe mit einer Fehlermeldung.
: .Text:	( u -- )	\ u = Nummer des Screens
	BEGIN
		DUP (.Text)	\ Ausgabe des Screens
	WHILE			\ Abbruch bei Flag = 0
		1+		\ nächster Screen
	REPEAT ;

: LPT:	1 OUTDEV ! ;	\ Ausgabe nur auf dem Plotter
: ;CRT	0 OUTDEV ! ;	\ Ausgabe nur auf dem Monitor

: LIST/P	( u -- )	\ LIST nur auf dem Drucker
	LPT: Text kleine Schriftart CR' 19 L/P LIST ;CRT ;
: -LIST/P	( u1 u2 -- )	\ vgl. LIST/P und -LIST
	1+ SWAP DO I LIST/P LOOP ;

: +Protocol	\ Protokollfunktion ein (Steuerung mit CTRL-P)
	1 PROTOCOL ! ;
: -Protocol	\ Protokollfunktion aus (CTRL-P wirkungslos)
	0 PROTOCOL ! ;


\ Graphikausgabe mit den plottereigenen Steuercodes.
\ Ende durch Endkennung (Vorgabe: ";") oder Screenende.
: (.Graphik)	\ vgl. Codierung von (.")
	R COUNT DUP 1+ R> + R> TYPE CR' ;
: .Graphik:	\ vgl. Codierung von ."
	ENDE STATE @ IF POSTPONE (.Graphik)
						 WORD HERE C@ 1+ ALLOT
					 ELSE WORD HERE COUNT TYPE CR'
					 ENDIF ;

\ AB HIER NEUE DEFINITIONEN, DIE NICHT IM MZ-FORTH CODE VORKAMEN.

\ Kopfbewegungen
: links	( u -- ) \ Plotterstift um u Zeichen nach links bewegen.
		\ Ist der Plotterkopf am linken Seitenrand, wird diese
		\ Aufforderung ignoriert.
	ABS	\ Paranoia
	0 DO
		14 EMIT	\ Steuercode 0x0E
	LOOP ;

\ Graphikkommandos
\ NUR Für Graphikausgaben außerhalb von ".Graphik:".
: GRAPHIK		\ Graphikmodus einschalten.
	2 EMIT ;
: TEXT			\ In Textmodus schalten; 40 Zeichen/Zeile.
	ASCII A EMIT ;
: LINIEN-TYP	( b -- )	\ 0 <= b <= 15
				\ Linientyp: b =  0: durchgehende Linie, bis
				\            b = 15: punktierte Linie
	ASCII L EMIT . ;
: URPSRUNG			\ Stift anheben und zur Ausgangsposition zurückbringen.
	ASCII H EMIT ;
: URSPRUNG!		\ Aktuelle Stiftposition wird der Ursprung (x=0, y=0).
	ASCII I EMIT ;
: LINIE			( x y -- )	\ -999 <= x,y <= 999
					\ Zeichnet eine Linie von der aktuellen Stiftpos. nach (x,y).
	SWAP				\ x muß "D" bei der Ausgabe folgen.
	ASCII D EMIT . ASCII , EMIT . ;
: RLINIE			( dx dy -- )	\ -999 <= dx,dy <= 999
						\ wie "LINIE"; dx und dy sind aber relative Koordinaten.
	SWAP			\ dx muß "J" bei der Ausgabe folgen.
	ASCII J EMIT . ASCII , EMIT . ;
: BEWEGEN		( x y -- )	\ -999 <= x,y <= 999
					\ Bewegt den Stift an die Position (x,y).
	SWAP
	ASCII M EMIT . ASCII , EMIT . ;
: RBEWEGEN		( dx dy -- )	\ -999 <= dx,dy <= 999
					\ wie "BEWEGEN"; dx und dy sind aber relative Koordinaten.
	SWAP				\ dx muß "R" bei der Ausgabe folgen.
	ASCII R EMIT . ASCII , EMIT . ;
: FARBE!			( b -- )	\ 0 <= c <= 3
						\ Farbe wechseln, s.a. "+Farbe" und "setzen".
	DUP
	ASCII C EMIT .
	Farbe ! ;
: SKALIERUNG	( b -- )	\ 0 <= b <= 63; Zeichengröße festlegen.
	ASCII S EMIT . ;
: TEXT-DREHEN	( b -- )	\ 0 <= b <= 3; Textrichtung drehen (90° Schritte).
	ASCII Q EMIT . ;
: ZEICHEN	( c -- )	\ Plottet das Zeichen c.
	ASCII P EMIT EMIT ;
: ACHSE	( p q r -- )	\ p = 0,1; -999 <= q <= 999; r = 1 bis 255
					\ Zeichnet eine x- (p=1) bzw. y-Achse (p=0) mit der
					\ Skaleneinteilung q und r Skalenmarkierungen.
	ASCII X EMIT . ASCII , EMIT . ASCII , EMIT . ;


FORTH DEFINITIONS

\ vim: set ts=3 sw=3 nocindent:

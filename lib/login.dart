import 'dart:ui';

import 'package:clippy/sharedwidgets.dart';
import 'package:clippy/utils.dart';
import 'package:flutter/material.dart';

class LoginPage extends StatefulWidget {
  const LoginPage({super.key});

  @override
  State<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends State<LoginPage> {
  @override
  Widget build(BuildContext context) {
    return Stack(
      children: [
        Positioned.fill(
          child: BackdropFilter(
            filter: ImageFilter.blur(
              sigmaX: 8,
              sigmaY: 8,
            ),
          ),
        ),
        Container(
          decoration: BoxDecoration(
            image: DecorationImage(
              image: AssetImage("assets/login.jpg"),
              fit: BoxFit.cover,
            ),
          ),
          child: Container(
            decoration: BoxDecoration(
              gradient: RadialGradient(
                colors: [
                  AppColours.loginBgStop1,
                  AppColours.loginBgStop2,
                ],
              ),
            ),
          ),
        ),
        Align(
          alignment: Alignment.center,
          child: Container(
            padding: EdgeInsets.all(32),
            margin: EdgeInsets.all(16),
            constraints: BoxConstraints(
              maxWidth: 500,
            ),
            decoration: BoxDecoration(
              color: Colors.white,
              borderRadius: BorderRadius.circular(8),
            ),
            child: Column(
              mainAxisAlignment: MainAxisAlignment.start,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(
                  "Welcome to Clippy",
                  textAlign: TextAlign.start,
                  style: TextStyle(
                    color: AppColours.dark,
                    fontWeight: FontWeight.bold,
                    fontSize: 32,
                  ),
                ),
                SizedBox(
                  height: 16,
                ),
                Text(
                  "Login to start saving your clipboard!",
                  textAlign: TextAlign.start,
                  style: TextStyle(
                    color: AppColours.dark,
                    fontWeight: FontWeight.normal,
                    fontSize: 16,
                  ),
                ),
                SizedBox(
                  height: 16,
                ),
                FormEntry(),
                SizedBox(
                  height: 16,
                ),
                FormEntry(),
                SizedBox(
                  height: 16,
                ),
                ButtonElement(),
                SizedBox(
                  height: 16,
                ),
                Text(
                  "Or",
                  textAlign: TextAlign.center,
                  style: TextStyle(
                    fontSize: 16,
                    fontWeight: FontWeight.normal,
                    color: AppColours.dark,
                  ),
                ),
                SizedBox(
                  height: 16,
                ),
                Text(
                  "sign up",
                  textAlign: TextAlign.center,
                  style: const TextStyle(
                    fontSize: 16,
                    fontWeight: FontWeight.normal,
                    color: Color(0xFF0078D4),
                    decoration: TextDecoration.underline,
                  ),
                ),
              ],
            ),
          ),
        )
      ],
    );
  }
}

class FormEntry extends StatefulWidget {
  const FormEntry({super.key});

  @override
  State<FormEntry> createState() => _FormEntryState();
}

class _FormEntryState extends State<FormEntry> {
  @override
  Widget build(BuildContext context) {
    return Material(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.start,
        crossAxisAlignment: CrossAxisAlignment.start,
        mainAxisSize: MainAxisSize.min,
        children: [
          Text(
            "Email address",
            style: TextStyle(
              color: AppColours.dark,
              fontSize: 12,
            ),
          ),
          SizedBox(
            height: 8,
          ),
          TextField(
            keyboardType: TextInputType.emailAddress,
            decoration: InputDecoration(
              hintText: "Email address",
            ),
          ),
        ],
      ),
    );
  }
}

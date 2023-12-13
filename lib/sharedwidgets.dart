import 'package:clippy/utils.dart';
import 'package:flutter/material.dart';

class ButtonElement extends StatefulWidget {
  EdgeInsets? padding = EdgeInsets.symmetric(
    horizontal: 32,
    vertical: 16,
  );
  Color? background = AppColours.dark;
  BorderRadius? radius = BorderRadius.circular(8);
  TextStyle? textStyle = TextStyle(
    color: Colors.white,
    fontSize: 16,
    fontWeight: FontWeight.normal,
  );
  String? text = "Login";
  ButtonElement({super.key});

  @override
  State<ButtonElement> createState() => _ButtonElementState();
}

class _ButtonElementState extends State<ButtonElement> {
  @override
  Widget build(BuildContext context) {
    return Container(
      padding: widget.padding,
      decoration: BoxDecoration(
        borderRadius: widget.radius,
        color: widget.background,
      ),
      child: Center(
        child: FittedBox(
          child: Text(
            widget.text ?? "",
            style: widget.textStyle,
          ),
        ),
      ),
    );
  }
}

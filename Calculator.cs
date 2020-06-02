using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Vrkshop
{
    /// <summary>
    /// Calculator class
    /// </summary>
    public class Calculator : MonoBehaviour
    {
        #region PRIVATE SERIALIZED FIELDS

        /// <summary>
        /// TextMesh pro UGUI for display text (primarily used for debugging)
        /// </summary>
        [SerializeField, Tooltip("TextMesh pro UGUI for display text (primarily used for debugging)")]
        private TextMeshProUGUI displayText = null;

        /// <summary>
        /// Maximum display characters
        /// </summary>
        [SerializeField, Tooltip("Maximum display characters")]
        private int maximumDisplayCharacters = 8;

        /// <summary>
        /// Display sprites
        /// </summary>
        [SerializeField, Tooltip("Display sprites")]
        private List<Sprite> displaySprites = new List<Sprite>();

        /// <summary>
        /// Display ui images, in order from right to left
        /// </summary>
        [SerializeField, Tooltip("Display ui images, in order from right to left")]
        private List<Image> displayUIImages = new List<Image>();

        /// <summary>
        /// Display ui dots, in order from right to left
        /// </summary>
        [SerializeField, Tooltip("Display ui dots, in order from right to left")]
        private List<Image> displayUIDots = new List<Image>();

        /// <summary>
        /// Audio clip for button click
        /// </summary>
        [SerializeField, Tooltip("Button click audio clip")]
        private AudioClip buttonClick = null;

        #endregion

        #region PRIVATE ENUMS

        /// <summary>
        /// Enum for the Calculator operators
        /// </summary>
        private enum CalculatorOperator { None, Add, Subtract, Multiply, Divide };

        /// <summary>
        /// Enum for the Calculator actions
        /// </summary>
        private enum CalculatorAction { None, Input, Equals, Operator, Error };

        #endregion

        #region PRIVATE FIELDS

        /// <summary>
        /// String builder used for storing the display value
        /// </summary>
        private StringBuilder displayString = new StringBuilder();

        /// <summary>
        /// Variable for the left hand side of calculation
        /// </summary>
        private double leftValue;

        /// <summary>
        /// Flag used to determine if leftValue exists
        /// </summary>
        private bool leftValueExists = false;

        /// <summary>
        /// Variable for the right hand side of calculation
        /// </summary>
        private double rightValue;

        /// <summary>
        /// Cache for memory value
        /// </summary>
        private double memoryValue;

        /// <summary>
        /// Flags used to determine if memory exists
        /// </summary>
        private bool memoryExists = false;

        /// <summary>
        /// State variable for active operator
        /// </summary>
        private CalculatorOperator activeOperator = CalculatorOperator.None;

        /// <summary>
        /// State variable for last action taken
        /// </summary>
        private CalculatorAction lastAction = CalculatorAction.None;

        /// <summary>
        /// Sprite map for display
        /// </summary>
        private Dictionary<string, Sprite> spriteMap = new Dictionary<string, Sprite>();

        /// <summary>
        /// Cached char stack used to set display
        /// </summary>
        private Stack<char> charStack = new Stack<char>();

        /// <summary>
        /// Flag used to determine if the display should be reset on next action
        /// </summary>
        private bool wipeOnNextInput = false;

        /// <summary>
        /// Smallest positive value able to be displayed
        /// </summary>
        private double smallPosValue;

        /// <summary>
        /// Smallest negative value able to be displayed
        /// </summary>
        private double smallNegValue;

        /// <summary>
        /// Largest positive value able to be displayed
        /// </summary>
        private double largePosValue;

        /// <summary>
        /// Largest negative value able to be displayed
        /// </summary>
        private double largeNegValue;

        /// <summary>
        /// Timer for button press cooldown
        /// </summary>
        private float coolDownTimer = 0f;

        /// <summary>
        /// Button press limit time
        /// </summary>
        private float buttonPressTimeLimit = 0.2f;



        #endregion

        #region PRIVATE UNITY LIFECYCLE METHODS

        private void Awake()
        {
            BuildSpriteMap();
            AllClear();

            double scale = System.Math.Pow(10, maximumDisplayCharacters - 1);

            smallPosValue = 1 / scale;
            smallNegValue = -10 / scale;
            largePosValue = 9 * scale;
            largeNegValue = -0.9 * scale;
        }

        #endregion

        #region PUBLIC METHODS

        /// <summary>
        /// Append a number string to the current string
        /// </summary>
        /// <param name="numberString"></param>
        public void AppendNumberString(string numberString)
        {
            if (!CheckIfCanPressButton())
            {
                return;
            }

            AudioManager.Instance.PlayClip(buttonClick, transform.position, .3f, 1f);

            if (lastAction == CalculatorAction.Error)
            {
                return;
            }

            if (lastAction != CalculatorAction.Input)
            {
                SetCurrentString(0);
            }

            bool hasDecimal = displayString.ToString().Contains(".");

            int maxLength = hasDecimal ? maximumDisplayCharacters + 1 : maximumDisplayCharacters;

            if (displayString.Length < maxLength)
            {
                if (ParseDisplayStringValue() == 0 && !displayString.ToString().Contains(".") || wipeOnNextInput)
                {
                    displayString.Clear();
                    wipeOnNextInput = false;
                }
                displayString.Append(numberString);
                UpdateDisplayText();
                lastAction = CalculatorAction.Input;
            }
        }

        /// <summary>
        /// Add a decimal to the current string
        /// </summary>
        public void Decimal()
        {
            if (!CheckIfCanPressButton())
            {
                return;
            }

            AudioManager.Instance.PlayClip(buttonClick, transform.position, .3f, 1f);

            if (lastAction == CalculatorAction.Error)
            {
                return;
            }

            if (lastAction == CalculatorAction.Equals || lastAction == CalculatorAction.Operator)
            {
                displayString.Clear();
                displayString.Append("0");
            }

            if (displayString.ToString().Contains("."))
            {
                return;
            }
            else
            {
                displayString.Append(".");
                UpdateDisplayText();
                lastAction = CalculatorAction.Input;
            }
        }

        /// <summary>
        /// Square root button function
        /// </summary>
        public void SquareRoot()
        {
            if (!CheckIfCanPressButton())
            {
                return;
            }

            AudioManager.Instance.PlayClip(buttonClick, transform.position, .3f, 1f);

            if (lastAction == CalculatorAction.Error)
            {
                return;
            }

            lastAction = CalculatorAction.Input;
            SetCurrentString(System.Math.Sqrt(ParseDisplayStringValue()));
            wipeOnNextInput = true;
        }

        /// <summary>
        /// Square button function
        /// </summary>
        public void Squared()
        {
            if (!CheckIfCanPressButton())
            {
                return;
            }

            AudioManager.Instance.PlayClip(buttonClick, transform.position, .3f, 1f);

            if (lastAction == CalculatorAction.Error)
            {
                return;
            }

            double v = ParseDisplayStringValue();

            lastAction = CalculatorAction.Input;
            SetCurrentString(v * v);
            wipeOnNextInput = true;
        }

        /// <summary>
        /// Flip the sign button function
        /// </summary>
        public void FlipSign()
        {
            if (!CheckIfCanPressButton())
            {
                return;
            }

            AudioManager.Instance.PlayClip(buttonClick, transform.position, .3f, 1f);

            if (lastAction == CalculatorAction.Error)
            {
                return;
            }

            double v = ParseDisplayStringValue();
            if (v == 0f)
            {
                // Do nothing
            }
            else if (v < 0f)
            {
                displayString.Remove(0, 1);
                UpdateDisplayText();
                lastAction = CalculatorAction.Input;
            }
            else
            {
                if (displayString.Length > maximumDisplayCharacters - 1)
                {
                    if (!displayString.ToString().Contains("."))
                    {
                        Error();
                        return;
                    }
                }
                displayString.Insert(0, "-");
                UpdateDisplayText();
                lastAction = CalculatorAction.Input;
            }
        }

        /// <summary>
        /// Memory recall button function
        /// </summary>
        public void MemoryRecall()
        {
            if (!CheckIfCanPressButton())
            {
                return;
            }

            AudioManager.Instance.PlayClip(buttonClick, transform.position, .3f, 1f);

            if (lastAction == CalculatorAction.Error)
            {
                return;
            }
            if (memoryExists)
            {
                SetCurrentString(memoryValue);
                lastAction = CalculatorAction.Input;
                wipeOnNextInput = true;
            }
        }

        /// <summary>
        /// Memory add button function
        /// </summary>
        public void MemoryAdd()
        {
            if (!CheckIfCanPressButton())
            {
                return;
            }

            AudioManager.Instance.PlayClip(buttonClick, transform.position, .3f, 1f);

            if (lastAction == CalculatorAction.Error)
            {
                return;
            }
            memoryValue += ParseDisplayStringValue();
            memoryExists = true;
        }

        /// <summary>
        /// Memory subtract button function
        /// </summary>
        public void MemorySubtract()
        {
            if (!CheckIfCanPressButton())
            {
                return;
            }

            AudioManager.Instance.PlayClip(buttonClick, transform.position, .3f, 1f);

            if (lastAction == CalculatorAction.Error)
            {
                return;
            }
            memoryValue -= ParseDisplayStringValue();
            memoryExists = true;
        }

        /// <summary>
        /// Add button function
        /// </summary>
        public void Add()
        {
            if (!CheckIfCanPressButton())
            {
                return;
            }

            AudioManager.Instance.PlayClip(buttonClick, transform.position, .3f, 1f);

            ProcessOperator();

            activeOperator = CalculatorOperator.Add;
        }

        /// <summary>
        /// Subtract operator function
        /// </summary>
        public void Subtract()
        {
            if (!CheckIfCanPressButton())
            {
                return;
            }

            AudioManager.Instance.PlayClip(buttonClick, transform.position, .3f, 1f);

            ProcessOperator();

            activeOperator = CalculatorOperator.Subtract;
        }

        /// <summary>
        /// Multiply operator function
        /// </summary>
        public void Multiply()
        {
            if (!CheckIfCanPressButton())
            {
                return;
            }

            AudioManager.Instance.PlayClip(buttonClick, transform.position, .3f, 1f);

            ProcessOperator();

            activeOperator = CalculatorOperator.Multiply;
        }

        /// <summary>
        /// Divide operator function
        /// </summary>
        public void Divide()
        {
            if (!CheckIfCanPressButton())
            {
                return;
            }

            AudioManager.Instance.PlayClip(buttonClick, transform.position, .3f, 1f);

            ProcessOperator();

            activeOperator = CalculatorOperator.Divide;
        }

        /// <summary>
        /// Equals button function
        /// </summary>
        public void Equals()
        {
            if (!CheckIfCanPressButton())
            {
                return;
            }

            AudioManager.Instance.PlayClip(buttonClick, transform.position, .3f, 1f);

            if (lastAction == CalculatorAction.Error)
            {
                return;
            }

            Calculate();

            leftValueExists = false;

        }

        /// <summary>
        /// Clear the current display
        /// </summary>
        public void Clear()
        {
            if (!CheckIfCanPressButton())
            {
                return;
            }

            AudioManager.Instance.PlayClip(buttonClick, transform.position, .3f, 1f);

            SetCurrentString(0);
            rightValue = 0f;
            lastAction = CalculatorAction.Input;
        }

        /// <summary>
        /// Clear the current display and stored values
        /// </summary>
        public void AllClear()
        {
            if (!CheckIfCanPressButton())
            {
                return;
            }

            AudioManager.Instance.PlayClip(buttonClick, transform.position, .3f, 1f);

            leftValue = 0f;
            leftValueExists = false;
            rightValue = 0f;

            memoryValue = 0f;
            memoryExists = false;

            SetCurrentString(0);
            lastAction = CalculatorAction.None;
            activeOperator = CalculatorOperator.None;
        }

        #endregion

        #region PRIVATE METHODS

        /// <summary>
        /// Build the sprite map used for the UI display
        /// </summary>
        private void BuildSpriteMap()
        {
            for (int i = 0; i < 10; i++)
            {
                spriteMap.Add(i.ToString(), displaySprites[i]);
            }

            spriteMap.Add("blank", displaySprites[10]);
            spriteMap.Add("-", displaySprites[11]);
            spriteMap.Add("dot-off", displaySprites[12]);
            spriteMap.Add("dot-on", displaySprites[13]);
            spriteMap.Add("E", displaySprites[14]);
            spriteMap.Add("r", displaySprites[15]);


        }

        // Set error state
        private void Error()
        {
            displayString.Clear();
            displayString.Append("Err");
            UpdateDisplayText();
            lastAction = CalculatorAction.Error;
            wipeOnNextInput = true;
        }

        /// <summary>
        /// Formats a provided value to fit the display size
        /// </summary>
        /// <param name="value"></param>
        /// <returns>Returns bool as to whether the formatting was successful or not</returns>
        private bool FormatValue(double value)
        {
            return true;
        }

        /// <summary>
        /// Parse the value based on the display string
        /// </summary>
        private double ParseDisplayStringValue()
        {
            return double.Parse(displayString.ToString());
        }

        // Calculate the expression
        private void Calculate()
        {
            if (activeOperator == CalculatorOperator.None)
            {
                return;
            }

            switch (lastAction)
            {
                case CalculatorAction.Operator:
                    rightValue = leftValue;
                    break;
                case CalculatorAction.Input:
                    rightValue = ParseDisplayStringValue();
                    break;
                case CalculatorAction.Equals:
                    // rightValue stays the same
                    break;
                case CalculatorAction.None:
                    return;
            }

            // Set lastAction early in case Error happens in SetCurrentString()
            // so that Error state can propagate
            lastAction = CalculatorAction.Equals;

            switch (activeOperator)
            {
                case CalculatorOperator.Add:
                    leftValue += rightValue;
                    SetCurrentString(leftValue);
                    break;
                case CalculatorOperator.Subtract:
                    leftValue -= rightValue;
                    SetCurrentString(leftValue);
                    break;
                case CalculatorOperator.Multiply:
                    leftValue *= rightValue;
                    SetCurrentString(leftValue);
                    break;
                case CalculatorOperator.Divide:

                    if (rightValue != 0f)
                    {
                        leftValue /= rightValue;
                    }
                    else
                    {
                        Error();
                        return;
                    }
                    SetCurrentString(leftValue);
                    break;
            }

        }

        /// <summary>
        /// Process operator input
        /// </summary>
        private void ProcessOperator()
        {
            if (lastAction == CalculatorAction.Operator || lastAction == CalculatorAction.Error)
            {
                return;
            }
            else if (leftValueExists)
            {
                Calculate();
            }

            leftValue = ParseDisplayStringValue();
            leftValueExists = true;
            lastAction = CalculatorAction.Operator;
        }

        /// <summary>
        /// Set the current string to a provided value
        /// </summary>
        /// <param name="value"></param>
        private void SetCurrentString(double value)
        {
            if (value == 0 || (value >= smallPosValue && value <= largePosValue) || (value <= smallNegValue && value >= largeNegValue))
            {
                displayString.Clear();
                displayString.Append(value.ToString());
                UpdateDisplayText();
            }
            else
            {
                Error();
                return;
            }
        }

        /// <summary>
        /// Update the display text
        /// </summary>
        private void UpdateDisplayText()
        {
            string tempString = displayString.ToString();

            if (displayText != null)
            {
                displayText.text = tempString;
            }

            int decimalIndex = 0;

            if (tempString != "Err")
            {
                if (tempString.Length > maximumDisplayCharacters && !tempString.Contains("."))
                {
                    // whole number larger than display characters
                    Error();
                    return;
                }
                else
                {
                    tempString = ProcessNumberString(tempString, out decimalIndex);
                }
            }

            SetDisplay(tempString, decimalIndex);
        }

        /// <summary>
        /// Process a number string to fit the display
        /// This takes into account scientific notation and decimals
        /// </summary>
        /// <param name="input">Input string</param>
        /// <param name="decimalIndex"></param>
        /// <returns></returns>
        private string ProcessNumberString(string input, out int decimalIndex)
        {
            int decimalLocation = input.IndexOf('.');

            bool hasDecimal = decimalLocation != -1;

            bool hasE = input.Contains("E");

            // set default state of decimal index
            decimalIndex = 0;

            if (hasE)
            {
                bool negative = input.IndexOf("-") == 0;

                if (negative)
                {
                    input = input.Remove(0, 1);
                }

                int eIndex = input.IndexOf("E");
                int power = int.Parse(input.Substring(eIndex + 2));

                input = input.Substring(0, eIndex);

                if (hasDecimal)
                {
                    input = input.Remove(1, 1);
                }

                for (int i = 0; i < power - 1; i++)
                {
                    input = "0" + input;
                }

                if (negative)
                {
                    input = "-0." + input;
                }
                else
                {
                    input = "0." + input;
                }

                decimalLocation = negative ? 2 : 1;
            }

            if (hasDecimal || hasE)
            {

                if (input.Length > maximumDisplayCharacters + 1)
                {
                    input = input.Substring(0, maximumDisplayCharacters + 1);
                }

                // Calculate the index of the decimal place (0 value = far right of display)
                decimalIndex = input.Length - decimalLocation - 1;

                // Remove the decimal from the string
                input = input.Remove(decimalLocation, 1);
            }

            return input;
        }

        /// <summary>
        /// Set display to string value with provided decimal index
        /// </summary>
        /// <param name="input">string to display (without decimal)</param>
        /// <param name="decimalIndex">decimal index, incrementing from 0, right to left</param>
        private void SetDisplay(string input, int decimalIndex)
        {
            charStack.Clear();
            foreach (char c in input)
            {
                charStack.Push(c);
            }

            for (int i = 0; i < maximumDisplayCharacters; i++)
            {
                if (charStack.Count == 0)
                {
                    displayUIImages[i].sprite = spriteMap["blank"];
                }
                else
                {
                    Sprite sprite = null;
                    spriteMap.TryGetValue(charStack.Pop().ToString(), out sprite);
                    if (sprite != null)
                    {
                        displayUIImages[i].sprite = sprite;
                    }
                    else
                    {
                        // sprite not found for display. Set error state
                        Error();
                        return;
                    }
                }

                if (i == decimalIndex)
                {
                    displayUIDots[i].sprite = spriteMap["dot-on"];
                }
                else
                {
                    displayUIDots[i].sprite = spriteMap["dot-off"];
                }
            }
        }

        /// <summary>
        /// Check if button is able to be pressed
        /// </summary>
        /// <returns></returns>
        private bool CheckIfCanPressButton()
        {
            if (coolDownTimer <= 0)
            {
                StartCoroutine(InitiateButtonCooldown());
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion

        #region PRIVATE COROUTINES

        /// <summary>
        /// Countdown timer to next button press
        /// </summary>
        /// <returns></returns>
        private IEnumerator InitiateButtonCooldown()
        {
            coolDownTimer = buttonPressTimeLimit;

            while (coolDownTimer > 0)
            {
                coolDownTimer -= Time.deltaTime;
                yield return null;
            }
        }

        #endregion
    }
}
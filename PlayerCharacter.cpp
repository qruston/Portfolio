/* Copyright (C) Quinton Ruston - All Rights Reserved 
* Unauthorized copying of this file, via any medium is strictly prohibited 
* Proprietary and confidential 
* Written by Quinton Ruston <qruston@gmail.com>, October 2020 */



#include "PlayerCharacter.h"
#include "CharacterAnimationBased.h"
#include "Kismet/GameplayStatics.h" // for GetPlayerController()
#include "Kismet/KismetMathLibrary.h"
#include "GameFramework/CharacterMovementComponent.h"

// Sets default values
APlayerCharacter::APlayerCharacter()
{
	// Set this character to call Tick() every frame.  You can turn this off to improve performance if you don't need it.
	PrimaryActorTick.bCanEverTick = true;

}

// Called when the game starts or when spawned
void APlayerCharacter::BeginPlay()
{
	Super::BeginPlay();
	GetCharacterMovement()->MaxWalkSpeed = SprintSpeed;
	CamManager = GetWorld()->GetFirstPlayerController()->PlayerCameraManager;
}

// Called every frame
void APlayerCharacter::Tick(float DeltaTime)
{
	Super::Tick(DeltaTime);
	if (CanMove)
	{
		NormalizedXAxis = NormalizeAxisValue(XAxisValue);//Normalize and store the X Axis 
		NormalizedYAxis = NormalizeAxisValue(YAxisValue);//Normalize and store the y Axis
		if (CameraLockOn)//If camera is in lock on Mode
		{
			//Rotate Character to face Camera direction
			UGameplayStatics::GetPlayerController(GetWorld(), 0)->SetControlRotation(CamManager->GetCameraRotation());//Rotate the Controller
			//Move Character based on camer direction
			if ((NormalizedXAxis + NormalizedYAxis) != 0)
			{
				FRotator ControlRot = GetControlRotation();
				ControlRot.Roll = 0;
				ControlRot.Pitch = 0;
				
				LastRotator = GetYRotatorValue(XAxisValue, YAxisValue);//Set the Y Rotator value
				LastRotator = GetXRotatorValue(XAxisValue, YAxisValue);//Set and lerp the X rotator value to the Y Rotator Value
				LastRotator += ControlRot;

				FVector MovementDirection;
				MovementDirection.X = YAxisValue;
				MovementDirection.Y = XAxisValue ;

				

				AddMovementInput(UKismetMathLibrary::TransformDirection(UGameplayStatics::GetPlayerPawn(GetWorld(), 0)->GetActorTransform(), MovementDirection));//Move the character forward
			}
		}
		else// If camera is in Normal Mode
		{
			if (ShouldRotate())//Check if the Character is able to rotate and move
			{
				LastRotator = GetYRotatorValue(XAxisValue, YAxisValue);//Set the Y Rotator value
				LastRotator = GetXRotatorValue(XAxisValue, YAxisValue);//Set and lerp the X rotator value to the Y Rotator Value
				LastRotator += CamManager->GetCameraRotation();
				UGameplayStatics::GetPlayerController(GetWorld(), 0)->SetControlRotation(LastRotator);//Rotate the Controller

				if ((NormalizedXAxis + NormalizedYAxis) != 0)
				{
					FRotator ControlRot = GetControlRotation();
					ControlRot.Roll = 0;
					ControlRot.Pitch = 0;

					AddMovementInput(UKismetMathLibrary::GetForwardVector(ControlRot));//Move the character forward
				}

			}
		}
	}
}

// Called to bind functionality to input
void APlayerCharacter::SetupPlayerInputComponent(UInputComponent* PlayerInputComponent)
{
	Super::SetupPlayerInputComponent(PlayerInputComponent);
	// set up gameplay key bindings
	check(InputComponent);
	InputComponent->BindAxis("MoveForward", this, &APlayerCharacter::ForwardBackwardInput);
	InputComponent->BindAxis("MoveRight", this, &APlayerCharacter::RightLeftInput);
	InputComponent->BindAction("Jump", EInputEvent::IE_Pressed, this, &APlayerCharacter::JumpPressedInput);
	InputComponent->BindAction("Jump", EInputEvent::IE_Released, this, &APlayerCharacter::JumpReleasedInput);
	InputComponent->BindAction("Sprint", EInputEvent::IE_Pressed, this, &APlayerCharacter::SprintPressedInput);
	InputComponent->BindAction("Sprint", EInputEvent::IE_Released, this, &APlayerCharacter::SprintReleasedInput);
	//InputComponent->BindAction("Crouch", EInputEvent::IE_Pressed, this, &APlayerCharacter::CrouchPressedInput);
	//InputComponent->BindAction("Crouch", EInputEvent::IE_Released, this, &APlayerCharacter::CrouchReleasedInput);
	InputComponent->BindAction("LightAttack", EInputEvent::IE_Pressed, this, &APlayerCharacter::AttackPressedInput);
	InputComponent->BindAction("LightAttack", EInputEvent::IE_Released, this, &APlayerCharacter::AttackReleasedInput);
}

float APlayerCharacter::NormalizeAxisValue(float RawAxis)
{
	if (RawAxis < 0)//Check if the raw axis is less then 0
	{
		return RawAxis * -1;
	}

	return RawAxis;
}

bool APlayerCharacter::ShouldRotate()
{
	if (XAxisValue == 0 && YAxisValue == 0)//Check if the Two axis values are 0 if they are no rotation should be done
	{
		return false;
	}

	return true;
}

FRotator APlayerCharacter::GetYRotatorValue(float xAxis, float yAxis)
{
	if (yAxis < 0)// If the yaxis is in the negatives 
	{
		return BackRotator;//return the back rotator 
	}
	else if (yAxis > 0)// if the y axis is in the positives
	{
		if (xAxis > 0)// if the xaxis is going to the right then the y axis has to be the neutral rotator(0) to successfully lerp in later stages
		{
			return NeutralRotator;
		}
		else// if the x axis is going to the left then the y axis has to use the Forward rotator (360) to successfully lerp in later stages
		{
			return ForwardRotator;
		}
	}
	else//if the no input on the y axis return the neutral rotator
	{
		return NeutralRotator;
	}
}

FRotator APlayerCharacter::GetXRotatorValue(float xAxis, float yAxis)
{
	if (xAxis > 0)//Check if the x axis is greater then 0 (going towards the right)
	{
		return FMath::Lerp(LastRotator, RightRotator, NormalizedXAxis / (NormalizedXAxis + NormalizedYAxis));//get the rotation between the forward or back rotator and the right rotator
	}
	else if (xAxis < 0)//Check if the x axis is less then 0 (going towards the left)
	{
		return FMath::Lerp(LastRotator, LeftRotator, NormalizedXAxis / (NormalizedXAxis + NormalizedYAxis));//get the rotation between the forward or back rotator and the Left rotator
	}
	else
	{
		return FMath::Lerp(LastRotator, NeutralRotator, 0);//get the rotation between the forward or back rotator and the neutral rotator
	}
}

void APlayerCharacter::ForwardBackwardInput(float AxisValue)
{
	YAxisValue = AxisValue;//Set the Y axis value
}

void APlayerCharacter::RightLeftInput(float AxisValue)
{
	XAxisValue = AxisValue;//Set the X Axis value
}

void APlayerCharacter::JumpPressedInput()
{
	if (CanMove)//Check if character is allowed to move 
	{
		JumpMaxHoldTime = JumpTime;//set the characters max hold time to the jump time 
		Jump();// Call characters built in jump 
	}
}

void APlayerCharacter::JumpReleasedInput()
{
	StopJumping();//Call Characters built in Stop Jumping 

}

void APlayerCharacter::SprintPressedInput()
{
	GetCharacterMovement()->MaxWalkSpeed = WalkSpeed;//Set the max walk speed to the walk speed
}

void APlayerCharacter::SprintReleasedInput()
{
	GetCharacterMovement()->MaxWalkSpeed = SprintSpeed;//Set the max walk speed to the sprint speed
}

void APlayerCharacter::CrouchPressedInput()
{
	Crouch();//Calls Characters built in crouch
}

void APlayerCharacter::CrouchReleasedInput()
{
	UnCrouch();//Calls Characters Built in uncrouch
}

void APlayerCharacter::AttackPressedInput()
{
	
}

void APlayerCharacter::AttackReleasedInput()
{
	//Blueprint Does stuff with the animations 	
}

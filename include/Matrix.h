#ifndef MATRIX_H
#define MATRIX_H

#include "pugixml.hpp"

class Matrix {
private:
	pugi::xml_node root;
	double a, b, c, d, tx, ty;
public:
	Matrix(pugi::xml_node& matrixNode);
	Matrix(const pugi::xml_node& matrixNode);
	~Matrix();
	Matrix(const Matrix& matrix);
	double getA() const;
	void setA(double a);
	double getB() const;
	void setB(double b);
	double getC() const;
	void setC(double c);
	double getD() const;
	void setD(double d);
	double getTx() const;
	void setTx(double tx);
	double getTy() const;
	void setTy(double ty);
	pugi::xml_node& getRoot();
};

#endif // MATRIX_H